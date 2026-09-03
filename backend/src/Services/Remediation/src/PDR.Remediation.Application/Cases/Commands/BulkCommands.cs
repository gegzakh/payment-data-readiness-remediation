using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.Remediation.Application.Abstractions;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Application.Cases.Commands;

/// <summary>Which cases a bulk action addresses. Never "everything" implicitly (FR-REM-007).</summary>
public sealed record BulkSelection(
    string? SourceCode = null,
    string? Queue = null,
    string? RuleCode = null,
    CaseStatus? Status = null,
    CasePriority? MinimumPriority = null,
    decimal? MinimumConfidence = null,
    IReadOnlyList<Guid>? CaseIds = null);

public sealed record PreviewBulkActionCommand(string Action, BulkSelection Selection) : ICommand<BulkPreviewDto>;

public sealed record ApplyBulkActionCommand(
    string Action,
    BulkSelection Selection,
    string? Rationale) : ICommand<BulkResultDto>;

public sealed class PreviewBulkActionCommandValidator : AbstractValidator<PreviewBulkActionCommand>
{
    public PreviewBulkActionCommandValidator() =>
        RuleFor(command => command.Action).NotEmpty().Must(BulkActions.IsKnown)
            .WithMessage($"Supported actions are {string.Join(", ", BulkActions.All)}.");
}

public sealed class ApplyBulkActionCommandValidator : AbstractValidator<ApplyBulkActionCommand>
{
    public ApplyBulkActionCommandValidator()
    {
        RuleFor(command => command.Action).NotEmpty().Must(BulkActions.IsKnown)
            .WithMessage($"Supported actions are {string.Join(", ", BulkActions.All)}.");
        RuleFor(command => command.Rationale).MaximumLength(1024);
    }
}

public static class BulkActions
{
    public const string Submit = "submit";
    public const string Approve = "approve";
    public const string Assign = "assign";

    public static readonly string[] All = [Submit, Approve, Assign];

    public static bool IsKnown(string action) =>
        All.Contains(action, StringComparer.OrdinalIgnoreCase);
}

internal static class BulkQuery
{
    public static IQueryable<RemediationCase> Apply(IQueryable<RemediationCase> query, BulkSelection selection)
    {
        if (!string.IsNullOrWhiteSpace(selection.SourceCode))
        {
            var sourceCode = selection.SourceCode.ToUpperInvariant();
            query = query.Where(entity => entity.SourceCode == sourceCode);
        }

        if (!string.IsNullOrWhiteSpace(selection.Queue))
        {
            query = query.Where(entity => entity.Queue == selection.Queue);
        }

        if (!string.IsNullOrWhiteSpace(selection.RuleCode))
        {
            query = query.Where(entity => entity.IssueRuleCodes.Contains(selection.RuleCode));
        }

        if (selection.Status is { } status)
        {
            query = query.Where(entity => entity.Status == status);
        }

        if (selection.MinimumPriority is { } priority)
        {
            query = query.Where(entity => entity.Priority >= priority);
        }

        if (selection.CaseIds is { Count: > 0 } ids)
        {
            query = query.Where(entity => ids.Contains(entity.Id));
        }

        return query;
    }
}

/// <summary>
/// Shows the population, the exposure and the blockers before anybody commits, and states whether the
/// action can be reversed (FR-REM-007).
/// </summary>
public sealed class PreviewBulkActionCommandHandler(
    IRemediationDbContext context,
    ISettingsReader settings,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<PreviewBulkActionCommand, Result<BulkPreviewDto>>
{
    public async Task<Result<BulkPreviewDto>> HandleAsync(
        PreviewBulkActionCommand request,
        CancellationToken cancellationToken)
    {
        var minimumConfidence = request.Selection.MinimumConfidence ?? await settings.GetAsync(
            RemediationSettingKeys.BulkApprovalMinimumConfidence,
            RemediationDefaults.BulkApprovalMinimumConfidence,
            cancellationToken);

        var matched = await BulkQuery
            .Apply(context.Cases.Include(entity => entity.Proposal).AsNoTracking(), request.Selection)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var blocked = new List<string>();
        var eligible = new List<RemediationCase>();

        foreach (var entity in matched)
        {
            var reason = BulkEligibility.Blocker(
                request.Action,
                entity,
                currentUser.UserName,
                minimumConfidence);

            if (reason is null)
            {
                eligible.Add(entity);
            }
            else
            {
                blocked.Add(reason);
            }
        }

        return new BulkPreviewDto(
            request.Action.ToLowerInvariant(),
            matched.Count,
            eligible.Count,
            matched.Count - eligible.Count,
            eligible.Sum(entity => entity.FutureExposure),
            eligible.Count == 0 ? null : eligible.Min(entity => entity.Proposal?.OverallConfidence ?? 0m),
            // Nothing has reached a source system yet, so every workflow action is reversible by decision.
            true,
            [.. blocked.GroupBy(reason => reason).OrderByDescending(group => group.Count())
                .Select(group => $"{group.Key} ({group.Count()})")],
            [.. eligible
                .OrderByDescending(entity => entity.PriorityScore)
                .Take(RemediationDefaults.BulkPreviewSampleSize)
                .Select(entity => entity.ToListItem(today))]);
    }
}

public sealed class ApplyBulkActionCommandHandler(
    IRemediationDbContext context,
    ISettingsReader settings,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<ApplyBulkActionCommand, Result<BulkResultDto>>
{
    public async Task<Result<BulkResultDto>> HandleAsync(
        ApplyBulkActionCommand request,
        CancellationToken cancellationToken)
    {
        var minimumConfidence = request.Selection.MinimumConfidence ?? await settings.GetAsync(
            RemediationSettingKeys.BulkApprovalMinimumConfidence,
            RemediationDefaults.BulkApprovalMinimumConfidence,
            cancellationToken);

        var slaDays = await settings.GetAsync(
            RemediationSettingKeys.SlaDays,
            RemediationDefaults.DefaultSlaDays,
            cancellationToken);

        var matched = await BulkQuery
            .Apply(context.Cases.Include(entity => entity.Proposal).Include(entity => entity.History), request.Selection)
            .ToListAsync(cancellationToken);

        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var applied = 0;
        var skipped = 0;
        var failures = new List<string>();

        foreach (var entity in matched)
        {
            var blocker = BulkEligibility.Blocker(request.Action, entity, currentUser.UserName, minimumConfidence);
            if (blocker is not null)
            {
                skipped++;
                continue;
            }

            var result = request.Action.ToLowerInvariant() switch
            {
                BulkActions.Submit => entity.Submit(currentUser.UserName, evidenceRequired: false, now),
                BulkActions.Approve => entity.Decide(
                    DecisionType.Approve,
                    currentUser.UserName,
                    request.Rationale,
                    exceptionExpiresOn: null,
                    now),
                _ => Assign(entity, request, slaDays, today, currentUser.UserName, now)
            };

            if (result.IsFailure)
            {
                failures.Add($"{entity.CaseKey}: {result.Error.Message}");
                skipped++;
                continue;
            }

            applied++;
        }

        await context.SaveChangesAsync(cancellationToken);

        return new BulkResultDto(request.Action.ToLowerInvariant(), applied, skipped, failures);
    }

    private static Result Assign(
        RemediationCase entity,
        ApplyBulkActionCommand request,
        int slaDays,
        DateOnly today,
        string actor,
        DateTimeOffset now)
    {
        entity.Assign(
            request.Selection.Queue ?? RemediationDefaults.DefaultQueue,
            request.Rationale,
            today.AddDays(slaDays),
            actor,
            now);

        return Result.Success();
    }
}

internal static class BulkEligibility
{
    /// <summary>Why this case cannot take part, or null when it can.</summary>
    public static string? Blocker(
        string action,
        RemediationCase entity,
        string actor,
        decimal minimumConfidence) =>
        action.ToLowerInvariant() switch
        {
            BulkActions.Submit when entity.Proposal is null => "no proposed correction",
            BulkActions.Submit when entity.Status is not (CaseStatus.New or CaseStatus.InProgress or CaseStatus.Returned) =>
                $"status is {entity.Status}",
            BulkActions.Submit when entity.Proposal!.OverallConfidence < minimumConfidence =>
                $"confidence below {minimumConfidence:0.##}",
            BulkActions.Approve when entity.Status != CaseStatus.PendingApproval => $"status is {entity.Status}",
            BulkActions.Approve when string.Equals(entity.SubmittedBy, actor, StringComparison.OrdinalIgnoreCase) =>
                "the caller submitted it",
            BulkActions.Approve when entity.Proposal!.OverallConfidence < minimumConfidence =>
                $"confidence below {minimumConfidence:0.##}",
            BulkActions.Approve when entity.Proposal.RequiresHumanVerification =>
                "machine-assisted proposals need individual review",
            BulkActions.Assign when entity.Status is CaseStatus.Remediated or CaseStatus.Dismissed =>
                $"status is {entity.Status}",
            _ => null
        };
}
