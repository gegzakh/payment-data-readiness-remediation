using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.Simulation.Application.Abstractions;
using PDR.Simulation.Application.Scenarios;
using PDR.Simulation.Application.Upstream;
using PDR.Simulation.Domain.Cutover;
using PDR.Simulation.Domain.Scenarios;
using PDR.Simulation.Domain.Testing;

namespace PDR.Simulation.Application.Cutover;

public sealed record CriterionDto(
    Guid Id,
    string Reference,
    CriterionKind Kind,
    string Description,
    string Owner,
    bool IsBlocking,
    CriterionStatus Status,
    string? EvidenceReference,
    string? Rationale,
    string? RecordedBy,
    DateTimeOffset? RecordedAtUtc);

public sealed record ApprovalDto(
    Guid Id,
    string Role,
    string Approver,
    ApprovalDecision Decision,
    string Rationale,
    GoNoGoRecommendation RecommendationAtSignOff,
    DateTimeOffset DecidedAtUtc);

public sealed record CutoverPlanDto(
    Guid Id,
    string Code,
    string Name,
    DateOnly CutoverDate,
    string Owner,
    DateOnly? FreezeFrom,
    DateOnly? FreezeTo,
    bool IsFrozen,
    string? FallbackPlan,
    string? SupportModel,
    IReadOnlyList<CriterionDto> Criteria,
    IReadOnlyList<ApprovalDto> Approvals);

/// <summary>
/// Everything a steering committee needs in one payload: residual exposure, outstanding exceptions, the
/// state of testing and operational readiness, and who has signed (FR-CUT-004).
/// </summary>
public sealed record GoNoGoPackDto(
    CutoverPlanDto Plan,
    GoNoGoRecommendation Recommendation,
    int ResidualExposure,
    int ResidualExposureTolerance,
    int PaymentsAtRisk,
    int OpenCases,
    int ExpiredExceptions,
    int OpenDefects,
    decimal TestCoveragePercent,
    int UatMismatches,
    int EntryCriteriaOutstanding,
    int ExitCriteriaOutstanding,
    int WaivedCriteria,
    Guid? BasedOnRunId,
    DateTimeOffset? BasedOnRunAtUtc,
    DateTimeOffset GeneratedAtUtc);

public sealed record CreateCutoverPlanCommand(string Code, string Name, DateOnly CutoverDate, string Owner)
    : ICommand<CutoverPlanDto>;

public sealed record SetOperationalPlanCommand(
    string Code,
    DateOnly? FreezeFrom,
    DateOnly? FreezeTo,
    string? FallbackPlan,
    string? SupportModel) : ICommand<CutoverPlanDto>;

public sealed record AddCriterionCommand(
    string Code,
    string Reference,
    CriterionKind Kind,
    string Description,
    string Owner,
    bool IsBlocking) : ICommand<CutoverPlanDto>;

public sealed record RecordCriterionCommand(
    string Code,
    string Reference,
    CriterionStatus Status,
    string? EvidenceReference,
    string? Rationale) : ICommand<CutoverPlanDto>;

public sealed record ApproveCutoverCommand(string Code, string Role, ApprovalDecision Decision, string Rationale)
    : ICommand<CutoverPlanDto>;

public sealed record GetCutoverPlansQuery : IQuery<IReadOnlyList<CutoverPlanDto>>;

public sealed record GetCutoverPlanQuery(string Code) : IQuery<CutoverPlanDto>;

public sealed record GetGoNoGoPackQuery(string Code) : IQuery<GoNoGoPackDto>;

public sealed class CreateCutoverPlanCommandValidator : AbstractValidator<CreateCutoverPlanCommand>
{
    public CreateCutoverPlanCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(140);
        RuleFor(command => command.Owner).NotEmpty().MaximumLength(140);
    }
}

public sealed class AddCriterionCommandValidator : AbstractValidator<AddCriterionCommand>
{
    public AddCriterionCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty();
        RuleFor(command => command.Reference).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(512);
        RuleFor(command => command.Owner).NotEmpty().MaximumLength(140);
    }
}

public sealed class ApproveCutoverCommandValidator : AbstractValidator<ApproveCutoverCommand>
{
    public ApproveCutoverCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty();
        RuleFor(command => command.Role).NotEmpty().MaximumLength(140);
        RuleFor(command => command.Rationale).NotEmpty().MaximumLength(1024);
    }
}

public static class CutoverMapper
{
    public static CutoverPlanDto ToDto(this CutoverPlan plan, DateOnly today) =>
        new(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.CutoverDate,
            plan.Owner,
            plan.FreezeFrom,
            plan.FreezeTo,
            plan.IsFrozen(today),
            plan.FallbackPlan,
            plan.SupportModel,
            [
                .. plan.Criteria
                    .OrderBy(item => item.Kind)
                    .ThenBy(item => item.Reference)
                    .Select(item => new CriterionDto(
                        item.Id,
                        item.Reference,
                        item.Kind,
                        item.Description,
                        item.Owner,
                        item.IsBlocking,
                        item.Status,
                        item.EvidenceReference,
                        item.Rationale,
                        item.RecordedBy,
                        item.RecordedAtUtc))
            ],
            [
                .. plan.Approvals
                    .OrderBy(item => item.Role)
                    .Select(item => new ApprovalDto(
                        item.Id,
                        item.Role,
                        item.Approver,
                        item.Decision,
                        item.Rationale,
                        item.RecommendationAtSignOff,
                        item.DecidedAtUtc))
            ]);
}

internal static class CutoverLoader
{
    public static Task<CutoverPlan?> LoadAsync(ISimulationDbContext context, string code, CancellationToken cancellationToken)
    {
        var normalized = code.ToUpperInvariant();
        return context.CutoverPlans
            .Include(plan => plan.Criteria)
            .Include(plan => plan.Approvals)
            .FirstOrDefaultAsync(plan => plan.Code == normalized, cancellationToken);
    }
}

public sealed class CreateCutoverPlanCommandHandler(ISimulationDbContext context, IClock clock)
    : IRequestHandler<CreateCutoverPlanCommand, Result<CutoverPlanDto>>
{
    public async Task<Result<CutoverPlanDto>> HandleAsync(CreateCutoverPlanCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        if (await context.CutoverPlans.AnyAsync(plan => plan.Code == code, cancellationToken))
        {
            return Result.Failure<CutoverPlanDto>(CutoverErrors.Duplicate(code));
        }

        var plan = CutoverPlan.Create(code, request.Name, request.CutoverDate, request.Owner);
        context.CutoverPlans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);
        return plan.ToDto(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
    }
}

public sealed class SetOperationalPlanCommandHandler(ISimulationDbContext context, IClock clock)
    : IRequestHandler<SetOperationalPlanCommand, Result<CutoverPlanDto>>
{
    public async Task<Result<CutoverPlanDto>> HandleAsync(SetOperationalPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await CutoverLoader.LoadAsync(context, request.Code, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<CutoverPlanDto>(CutoverErrors.NotFound(request.Code.ToUpperInvariant()));
        }

        plan.SetOperationalPlan(request.FreezeFrom, request.FreezeTo, request.FallbackPlan, request.SupportModel);
        await context.SaveChangesAsync(cancellationToken);
        return plan.ToDto(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
    }
}

public sealed class AddCriterionCommandHandler(ISimulationDbContext context, IClock clock)
    : IRequestHandler<AddCriterionCommand, Result<CutoverPlanDto>>
{
    public async Task<Result<CutoverPlanDto>> HandleAsync(AddCriterionCommand request, CancellationToken cancellationToken)
    {
        var plan = await CutoverLoader.LoadAsync(context, request.Code, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<CutoverPlanDto>(CutoverErrors.NotFound(request.Code.ToUpperInvariant()));
        }

        var added = plan.AddCriterion(request.Reference, request.Kind, request.Description, request.Owner, request.IsBlocking);
        if (added.IsFailure)
        {
            return Result.Failure<CutoverPlanDto>(added.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return plan.ToDto(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
    }
}

public sealed class RecordCriterionCommandHandler(ISimulationDbContext context, ICurrentUser currentUser, IClock clock)
    : IRequestHandler<RecordCriterionCommand, Result<CutoverPlanDto>>
{
    public async Task<Result<CutoverPlanDto>> HandleAsync(RecordCriterionCommand request, CancellationToken cancellationToken)
    {
        var plan = await CutoverLoader.LoadAsync(context, request.Code, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<CutoverPlanDto>(CutoverErrors.NotFound(request.Code.ToUpperInvariant()));
        }

        var recorded = plan.RecordCriterionStatus(
            request.Reference,
            request.Status,
            request.EvidenceReference,
            request.Rationale,
            currentUser.UserName,
            clock.UtcNow);

        if (recorded.IsFailure)
        {
            return Result.Failure<CutoverPlanDto>(recorded.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return plan.ToDto(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
    }
}

public sealed class ApproveCutoverCommandHandler(
    ISimulationDbContext context,
    GoNoGoPackBuilder packBuilder,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<ApproveCutoverCommand, Result<CutoverPlanDto>>
{
    public async Task<Result<CutoverPlanDto>> HandleAsync(ApproveCutoverCommand request, CancellationToken cancellationToken)
    {
        var plan = await CutoverLoader.LoadAsync(context, request.Code, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<CutoverPlanDto>(CutoverErrors.NotFound(request.Code.ToUpperInvariant()));
        }

        // The sign-off is stamped with the recommendation the approver is looking at right now.
        var pack = await packBuilder.BuildAsync(plan, cancellationToken);

        var approved = plan.Approve(
            request.Role,
            currentUser.UserName,
            request.Decision,
            request.Rationale,
            pack.Recommendation,
            clock.UtcNow);

        if (approved.IsFailure)
        {
            return Result.Failure<CutoverPlanDto>(approved.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return plan.ToDto(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
    }
}

public sealed class GetCutoverPlansQueryHandler(ISimulationDbContext context, IClock clock)
    : IRequestHandler<GetCutoverPlansQuery, Result<IReadOnlyList<CutoverPlanDto>>>
{
    public async Task<Result<IReadOnlyList<CutoverPlanDto>>> HandleAsync(
        GetCutoverPlansQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var plans = await context.CutoverPlans
            .AsNoTracking()
            .Include(plan => plan.Criteria)
            .Include(plan => plan.Approvals)
            .OrderBy(plan => plan.CutoverDate)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CutoverPlanDto>>([.. plans.Select(plan => plan.ToDto(today))]);
    }
}

public sealed class GetCutoverPlanQueryHandler(ISimulationDbContext context, IClock clock)
    : IRequestHandler<GetCutoverPlanQuery, Result<CutoverPlanDto>>
{
    public async Task<Result<CutoverPlanDto>> HandleAsync(GetCutoverPlanQuery request, CancellationToken cancellationToken)
    {
        var plan = await CutoverLoader.LoadAsync(context, request.Code, cancellationToken);
        return plan is null
            ? Result.Failure<CutoverPlanDto>(CutoverErrors.NotFound(request.Code.ToUpperInvariant()))
            : plan.ToDto(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
    }
}

public sealed class GetGoNoGoPackQueryHandler(ISimulationDbContext context, GoNoGoPackBuilder packBuilder)
    : IRequestHandler<GetGoNoGoPackQuery, Result<GoNoGoPackDto>>
{
    public async Task<Result<GoNoGoPackDto>> HandleAsync(GetGoNoGoPackQuery request, CancellationToken cancellationToken)
    {
        var plan = await CutoverLoader.LoadAsync(context, request.Code, cancellationToken);
        return plan is null
            ? Result.Failure<GoNoGoPackDto>(CutoverErrors.NotFound(request.Code.ToUpperInvariant()))
            : await packBuilder.BuildAsync(plan, cancellationToken);
    }
}

/// <summary>
/// Assembles the go/no-go pack from the latest remediated simulation, the remediation backlog and the test
/// plans, so the recommendation is always derived from evidence rather than typed in (FR-CUT-004).
/// </summary>
public sealed class GoNoGoPackBuilder(
    ISimulationDbContext context,
    IRemediationGateway remediation,
    ISettingsReader settings,
    IClock clock)
{
    public async Task<GoNoGoPackDto> BuildAsync(CutoverPlan plan, CancellationToken cancellationToken)
    {
        var tolerance = await settings.GetAsync(
            SimulationSettingKeys.ResidualExposureTolerance,
            SimulationDefaults.ResidualExposureTolerance,
            cancellationToken);

        var latestRemediatedRun = await context.Runs
            .AsNoTracking()
            .Where(run => run.Mode == ScenarioMode.Remediated && run.Status == RunStatus.Completed)
            .OrderByDescending(run => run.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var plans = await context.TestPlans
            .AsNoTracking()
            .Include(entity => entity.Cases)
            .ToListAsync(cancellationToken);

        var remediationSnapshot = await remediation.GetSnapshotAsync(cancellationToken);

        var residualExposure = Math.Max((latestRemediatedRun?.RejectedCount ?? 0) - tolerance, 0);
        var openDefects = plans.Sum(entity => entity.OpenDefectCount);
        var uatMismatches = plans.Sum(entity => entity.Cases.Count(item => item.UatOutcome == UatOutcome.Mismatch));
        var coverage = plans.Count == 0
            ? 0m
            : Math.Round(plans.Average(entity => entity.RiskWeightedCoveragePercent), 2);

        var recommendation = plan.Recommend(
            residualExposure,
            openDefects + uatMismatches,
            remediationSnapshot.ExpiredExceptions);

        return new GoNoGoPackDto(
            plan.ToDto(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)),
            recommendation,
            residualExposure,
            tolerance,
            latestRemediatedRun?.PaymentsAtRisk ?? remediationSnapshot.FutureExposureOpen,
            remediationSnapshot.OpenCases,
            remediationSnapshot.ExpiredExceptions,
            openDefects,
            coverage,
            uatMismatches,
            plan.Criteria.Count(item => item.Kind == CriterionKind.Entry && item.Status == CriterionStatus.Pending),
            plan.Criteria.Count(item => item.Kind == CriterionKind.Exit && item.Status == CriterionStatus.Pending),
            plan.Criteria.Count(item => item.Status == CriterionStatus.Waived),
            latestRemediatedRun?.Id,
            latestRemediatedRun?.CompletedAtUtc,
            clock.UtcNow);
    }
}
