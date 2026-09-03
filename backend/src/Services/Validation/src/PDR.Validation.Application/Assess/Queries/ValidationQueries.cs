using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.Validation.Application.Abstractions;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Application.Assess.Queries;

public sealed record GetRunsQuery(
    int Page = 1,
    int? PageSize = null,
    Guid? BatchId = null,
    string? SourceCode = null) : IQuery<PagedResult<ValidationRunDto>>;

public sealed record GetRunByIdQuery(Guid RunId) : IQuery<ValidationRunDto>;

public sealed record GetRunAssessmentsQuery(
    Guid RunId,
    int Page = 1,
    int? PageSize = null,
    RecordOutcome? Outcome = null,
    RuleMode Mode = RuleMode.Future,
    AddressClassification? Classification = null,
    string? RuleCode = null) : IQuery<PagedResult<AddressAssessmentDto>>;

public sealed record GetProfileQuery(ProfileDimension Dimension, Guid? RunId = null) : IQuery<ProfileDto>;

public sealed record GetReadinessSummaryQuery : IQuery<ReadinessSummaryDto>;

public static class ValidationDefaults
{
    public const string SchemeCode = "SEPA";
    public const int PageSize = 20;
    public const int MaxPageSize = 200;
    public const int TopIssueCount = 10;
}

internal static class LatestRuns
{
    /// <summary>Only the newest completed run per batch counts, so re-validating a batch replaces its exposure.</summary>
    public static Task<List<Guid>> IdsAsync(IValidationDbContext context, CancellationToken cancellationToken) =>
        context.Runs
            .AsNoTracking()
            .Where(run => run.Status == ValidationRunStatus.Completed)
            .GroupBy(run => run.BatchId)
            .Select(group => group.OrderByDescending(run => run.StartedAtUtc).Select(run => run.Id).First())
            .ToListAsync(cancellationToken);
}

internal static class ValidationPageSize
{
    public static async Task<int> ResolveAsync(ISettingsReader settings, int? requested, CancellationToken cancellationToken)
    {
        var configured = await settings.GetAsync(
            ValidationSettingKeys.PageSize,
            ValidationDefaults.PageSize,
            cancellationToken);

        return Math.Clamp(requested ?? configured, 1, ValidationDefaults.MaxPageSize);
    }
}

public sealed class GetRunsQueryHandler(IValidationDbContext context, ISettingsReader settings, IClock clock)
    : IRequestHandler<GetRunsQuery, Result<PagedResult<ValidationRunDto>>>
{
    public async Task<Result<PagedResult<ValidationRunDto>>> HandleAsync(
        GetRunsQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = await ValidationPageSize.ResolveAsync(settings, request.PageSize, cancellationToken);
        var page = Math.Max(request.Page, 1);

        var query = context.Runs.AsNoTracking();

        if (request.BatchId is { } batchId)
        {
            query = query.Where(run => run.BatchId == batchId);
        }

        if (!string.IsNullOrWhiteSpace(request.SourceCode))
        {
            var sourceCode = request.SourceCode.ToUpperInvariant();
            query = query.Where(run => run.SourceCode == sourceCode);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);

        var runs = await query
            .OrderByDescending(run => run.StartedAtUtc)
            .ThenBy(run => run.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ValidationRunDto>(
            [.. runs.Select(run => run.ToDto())],
            page,
            pageSize,
            totalCount,
            clock.UtcNow);
    }
}

public sealed class GetRunByIdQueryHandler(IValidationDbContext context)
    : IRequestHandler<GetRunByIdQuery, Result<ValidationRunDto>>
{
    public async Task<Result<ValidationRunDto>> HandleAsync(
        GetRunByIdQuery request,
        CancellationToken cancellationToken)
    {
        var run = await context.Runs
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.Id == request.RunId, cancellationToken);

        return run is null
            ? Result.Failure<ValidationRunDto>(ValidationErrors.RunNotFound(request.RunId))
            : run.ToDto();
    }
}

/// <summary>
/// Sampling and drill-down over one run's records. Values stay masked unless the caller holds the
/// drill-down permission (FR-VAL-009).
/// </summary>
public sealed class GetRunAssessmentsQueryHandler(
    IValidationDbContext context,
    ISettingsReader settings,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<GetRunAssessmentsQuery, Result<PagedResult<AddressAssessmentDto>>>
{
    public async Task<Result<PagedResult<AddressAssessmentDto>>> HandleAsync(
        GetRunAssessmentsQuery request,
        CancellationToken cancellationToken)
    {
        if (!await context.Runs.AnyAsync(run => run.Id == request.RunId, cancellationToken))
        {
            return Result.Failure<PagedResult<AddressAssessmentDto>>(ValidationErrors.RunNotFound(request.RunId));
        }

        var pageSize = await ValidationPageSize.ResolveAsync(settings, request.PageSize, cancellationToken);
        var page = Math.Max(request.Page, 1);
        var unmasked = currentUser.HasPermission(Permissions.Validation.DrillDown);

        var query = context.Assessments
            .AsNoTracking()
            .Include(assessment => assessment.Issues)
            .Where(assessment => assessment.RunId == request.RunId);

        if (request.Outcome is { } outcome)
        {
            query = request.Mode == RuleMode.Current
                ? query.Where(assessment => assessment.CurrentOutcome == outcome)
                : query.Where(assessment => assessment.FutureOutcome == outcome);
        }

        if (request.Classification is { } classification)
        {
            query = query.Where(assessment => assessment.Classification == classification);
        }

        if (!string.IsNullOrWhiteSpace(request.RuleCode))
        {
            var ruleCode = request.RuleCode.ToUpperInvariant();
            query = query.Where(assessment =>
                assessment.Issues.Any(issue => issue.RuleCode == ruleCode && issue.Mode == request.Mode));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);

        var assessments = await query
            .OrderBy(assessment => assessment.Sequence)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AddressAssessmentDto>(
            [.. assessments.Select(assessment => assessment.ToDto(unmasked))],
            page,
            pageSize,
            totalCount,
            clock.UtcNow);
    }
}

/// <summary>Breaks the assessed portfolio down by one dimension so exposure can be targeted (FR-VAL-006).</summary>
public sealed class GetProfileQueryHandler(IValidationDbContext context, IClock clock)
    : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> HandleAsync(GetProfileQuery request, CancellationToken cancellationToken)
    {
        // The profile has to describe the same population as the portfolio tiles: one run per batch and
        // assessed records only, so every row's count is the denominator of its readiness percentages.
        List<Guid> runIds = request.RunId is { } runId
            ? [runId]
            : await LatestRuns.IdsAsync(context, cancellationToken);

        var assessments = await context.Assessments
            .AsNoTracking()
            .Where(assessment => runIds.Contains(assessment.RunId))
            .Where(assessment => assessment.CurrentOutcome != RecordOutcome.Excluded
                && assessment.CurrentOutcome != RecordOutcome.UnableToAssess)
            .Select(assessment => new AssessmentFacts(
                assessment.Id,
                assessment.SchemeCode ?? "UNKNOWN",
                assessment.SourceCode,
                assessment.PartyRole.ToString(),
                assessment.Country ?? "UNKNOWN",
                assessment.Classification.ToString(),
                assessment.CurrentOutcome,
                assessment.FutureOutcome))
            .ToListAsync(cancellationToken);

        var rows = request.Dimension == ProfileDimension.Issue
            ? await IssueRowsAsync(assessments, cancellationToken)
            : assessments
                .GroupBy(assessment => Key(assessment, request.Dimension), StringComparer.Ordinal)
                .Select(group => Row(group.Key, group))
                .ToList();

        return new ProfileDto(
            request.Dimension,
            [.. rows.OrderByDescending(row => row.FutureRejectedCount).ThenBy(row => row.Key, StringComparer.Ordinal)],
            clock.UtcNow);
    }

    /// <summary>A record appears under every rule it breached, so rows overlap by design.</summary>
    private async Task<List<ProfileRowDto>> IssueRowsAsync(
        List<AssessmentFacts> assessments,
        CancellationToken cancellationToken)
    {
        var assessmentIds = assessments.Select(assessment => assessment.Id).ToList();

        var issues = await context.Issues
            .AsNoTracking()
            .Where(issue => assessmentIds.Contains(issue.AssessmentId))
            .Select(issue => new { issue.RuleCode, issue.AssessmentId })
            .Distinct()
            .ToListAsync(cancellationToken);

        var byId = assessments.ToDictionary(assessment => assessment.Id);

        return [.. issues
            .GroupBy(issue => issue.RuleCode, StringComparer.Ordinal)
            .Select(group => Row(group.Key, group.Select(issue => byId[issue.AssessmentId])))];
    }

    private static string Key(AssessmentFacts assessment, ProfileDimension dimension) => dimension switch
    {
        ProfileDimension.Scheme => assessment.SchemeCode,
        ProfileDimension.Source => assessment.SourceCode,
        ProfileDimension.PartyRole => assessment.PartyRole,
        ProfileDimension.Country => assessment.Country,
        _ => assessment.Classification
    };

    private static ProfileRowDto Row(string key, IEnumerable<AssessmentFacts> group)
    {
        var assessments = group.ToList();

        return new ProfileRowDto(
            key,
            assessments.Count,
            assessments.Count(assessment => assessment.Current == RecordOutcome.Rejected),
            assessments.Count(assessment => assessment.Future == RecordOutcome.Rejected),
            assessments.Count(assessment => assessment.Current == RecordOutcome.Warning),
            assessments.Count(assessment => assessment.Future == RecordOutcome.Warning),
            Percent(assessments.Count(assessment => assessment.Current == RecordOutcome.Compliant), assessments.Count),
            Percent(assessments.Count(assessment => assessment.Future == RecordOutcome.Compliant), assessments.Count));
    }

    private sealed record AssessmentFacts(
        Guid Id,
        string SchemeCode,
        string SourceCode,
        string PartyRole,
        string Country,
        string Classification,
        RecordOutcome Current,
        RecordOutcome Future);

    private static decimal Percent(int compliant, int assessed) =>
        assessed == 0 ? 0m : Math.Round(compliant * 100m / assessed, 2);
}

/// <summary>Portfolio readiness and the payments at risk after the cutover (FR-VAL-010).</summary>
public sealed class GetReadinessSummaryQueryHandler(
    IValidationDbContext context,
    ISettingsReader settings,
    IClock clock) : IRequestHandler<GetReadinessSummaryQuery, Result<ReadinessSummaryDto>>
{
    public async Task<Result<ReadinessSummaryDto>> HandleAsync(
        GetReadinessSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var topIssueCount = await settings.GetAsync(
            ValidationSettingKeys.TopIssueCount,
            ValidationDefaults.TopIssueCount,
            cancellationToken);

        var latestRunIds = await LatestRuns.IdsAsync(context, cancellationToken);

        var runs = await context.Runs
            .AsNoTracking()
            .Where(run => latestRunIds.Contains(run.Id))
            .ToListAsync(cancellationToken);

        var assessed = runs.Sum(run => run.AssessedCount);
        var currentCompliant = runs.Sum(run => run.CurrentCompliantCount);
        var futureCompliant = runs.Sum(run => run.FutureCompliantCount);

        var topIssues = await context.Issues
            .AsNoTracking()
            .Where(issue => context.Assessments
                .Any(assessment => assessment.Id == issue.AssessmentId && latestRunIds.Contains(assessment.RunId)))
            .GroupBy(issue => new { issue.RuleCode, issue.Field, issue.Severity, issue.Mode })
            .Select(group => new
            {
                group.Key.RuleCode,
                group.Key.Field,
                group.Key.Severity,
                group.Key.Mode,
                Count = group.Count()
            })
            .OrderByDescending(summary => summary.Count)
            .ThenBy(summary => summary.RuleCode)
            .Take(Math.Clamp(topIssueCount, 1, 100))
            .ToListAsync(cancellationToken);

        return new ReadinessSummaryDto(
            runs.Count,
            assessed,
            runs.Sum(run => run.ExcludedCount),
            runs.Sum(run => run.UnableToAssessCount),
            runs.Sum(run => run.CurrentRejectedCount),
            runs.Sum(run => run.FutureRejectedCount),
            Percent(currentCompliant, assessed),
            Percent(futureCompliant, assessed),
            runs.Sum(run => run.PaymentsAtRisk),
            [.. topIssues.Select(issue =>
                new IssueSummaryDto(issue.RuleCode, issue.Field, issue.Severity, issue.Mode, issue.Count))],
            clock.UtcNow);
    }

    private static decimal Percent(int compliant, int assessed) =>
        assessed == 0 ? 0m : Math.Round(compliant * 100m / assessed, 2);
}
