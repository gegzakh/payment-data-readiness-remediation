using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.Remediation.Application.Abstractions;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Application.Cases.Queries;

public sealed record GetCasesQuery(
    int Page = 1,
    int? PageSize = null,
    CaseStatus? Status = null,
    CasePriority? Priority = null,
    string? SourceCode = null,
    string? Queue = null,
    string? AssignedTo = null,
    string? RuleCode = null,
    Guid? CampaignId = null,
    bool OverdueOnly = false) : IQuery<PagedResult<CaseListItemDto>>;

public sealed record GetCaseByIdQuery(Guid CaseId) : IQuery<CaseDetailDto>;

public sealed record GetFunnelQuery : IQuery<RemediationFunnelDto>;

internal static class RemediationPageSize
{
    public static async Task<int> ResolveAsync(
        ISettingsReader settings,
        int? requested,
        CancellationToken cancellationToken)
    {
        var configured = await settings.GetAsync(
            RemediationSettingKeys.PageSize,
            RemediationDefaults.PageSize,
            cancellationToken);

        return Math.Clamp(requested ?? configured, 1, RemediationDefaults.MaxPageSize);
    }
}

public sealed class GetCasesQueryHandler(IRemediationDbContext context, ISettingsReader settings, IClock clock)
    : IRequestHandler<GetCasesQuery, Result<PagedResult<CaseListItemDto>>>
{
    public async Task<Result<PagedResult<CaseListItemDto>>> HandleAsync(
        GetCasesQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = await RemediationPageSize.ResolveAsync(settings, request.PageSize, cancellationToken);
        var page = Math.Max(request.Page, 1);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        var query = context.Cases.Include(entity => entity.Proposal).AsNoTracking();

        if (request.Status is { } status)
        {
            query = query.Where(entity => entity.Status == status);
        }

        if (request.Priority is { } priority)
        {
            query = query.Where(entity => entity.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(request.SourceCode))
        {
            var sourceCode = request.SourceCode.ToUpperInvariant();
            query = query.Where(entity => entity.SourceCode == sourceCode);
        }

        if (!string.IsNullOrWhiteSpace(request.Queue))
        {
            query = query.Where(entity => entity.Queue == request.Queue);
        }

        if (!string.IsNullOrWhiteSpace(request.AssignedTo))
        {
            query = query.Where(entity => entity.AssignedTo == request.AssignedTo);
        }

        if (!string.IsNullOrWhiteSpace(request.RuleCode))
        {
            query = query.Where(entity => entity.IssueRuleCodes.Contains(request.RuleCode));
        }

        if (request.CampaignId is { } campaignId)
        {
            query = query.Where(entity => entity.CampaignId == campaignId);
        }

        if (request.OverdueOnly)
        {
            query = query.Where(entity =>
                entity.DueDate != null
                && entity.DueDate < today
                && entity.Status != CaseStatus.Remediated
                && entity.Status != CaseStatus.Dismissed
                && entity.Status != CaseStatus.Rejected
                && entity.Status != CaseStatus.RolledBack);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);

        var cases = await query
            .OrderByDescending(entity => entity.PriorityScore)
            .ThenBy(entity => entity.DueDate)
            .ThenBy(entity => entity.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CaseListItemDto>(
            [.. cases.Select(entity => entity.ToListItem(today))],
            page,
            pageSize,
            totalCount,
            clock.UtcNow);
    }
}

public sealed class GetCaseByIdQueryHandler(IRemediationDbContext context, IClock clock)
    : IRequestHandler<GetCaseByIdQuery, Result<CaseDetailDto>>
{
    public async Task<Result<CaseDetailDto>> HandleAsync(
        GetCaseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Cases
            .Include(item => item.Proposal)
            .Include(item => item.Evidence)
            .Include(item => item.History)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.CaseId, cancellationToken);

        return entity is null
            ? Result.Failure<CaseDetailDto>(RemediationErrors.CaseNotFound(request.CaseId))
            : entity.ToDetail(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
    }
}

/// <summary>
/// The remediation funnel. Exceptions are reported separately and expired ones are called out, because
/// neither may be presented as compliant (FR-WF-007, FR-REP-001).
/// </summary>
public sealed class GetFunnelQueryHandler(IRemediationDbContext context, IClock clock)
    : IRequestHandler<GetFunnelQuery, Result<RemediationFunnelDto>>
{
    private static readonly CaseStatus[] OpenStatuses =
    [
        CaseStatus.New, CaseStatus.InProgress, CaseStatus.Returned,
        CaseStatus.PendingApproval, CaseStatus.Approved, CaseStatus.WriteBackPending, CaseStatus.Failed
    ];

    public async Task<Result<RemediationFunnelDto>> HandleAsync(
        GetFunnelQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        var facts = await context.Cases
            .AsNoTracking()
            .Select(entity => new CaseFacts(
                entity.SourceCode,
                entity.Priority,
                entity.Status,
                entity.FutureExposure,
                entity.DueDate,
                entity.ExceptionExpiresOn))
            .ToListAsync(cancellationToken);

        var total = facts.Count;
        var remediated = facts.Count(fact => fact.Status == CaseStatus.Remediated);

        return new RemediationFunnelDto(
            total,
            facts.Count(fact => OpenStatuses.Contains(fact.Status)),
            facts.Count(fact => fact.Status == CaseStatus.PendingApproval),
            facts.Count(fact => fact.Status == CaseStatus.Approved),
            remediated,
            facts.Count(fact => fact.Status == CaseStatus.Dismissed),
            facts.Count(fact => fact.Status == CaseStatus.Rejected),
            facts.Count(fact => fact.Status == CaseStatus.ExceptionGranted),
            facts.Count(fact => fact.Status == CaseStatus.ExceptionGranted
                                && fact.ExceptionExpiresOn != null
                                && fact.ExceptionExpiresOn < today),
            facts.Count(fact => fact.DueDate != null
                                && fact.DueDate < today
                                && OpenStatuses.Contains(fact.Status)),
            facts.Where(fact => OpenStatuses.Contains(fact.Status)).Sum(fact => fact.FutureExposure),
            facts.Where(fact => fact.Status == CaseStatus.Remediated).Sum(fact => fact.FutureExposure),
            total == 0 ? 0m : Math.Round(remediated * 100m / total, 2),
            [.. facts
                .GroupBy(fact => fact.Priority)
                .OrderByDescending(group => group.Key)
                .Select(group => Bucket(group.Key.ToString(), group))],
            [.. facts
                .GroupBy(fact => fact.SourceCode)
                .OrderByDescending(group => group.Sum(fact => fact.FutureExposure))
                .Select(group => Bucket(group.Key, group))],
            clock.UtcNow);
    }

    private static FunnelBucketDto Bucket(string key, IEnumerable<CaseFacts> group)
    {
        var facts = group.ToList();
        return new FunnelBucketDto(
            key,
            facts.Count,
            facts.Count(fact => OpenStatuses.Contains(fact.Status)),
            facts.Sum(fact => fact.FutureExposure));
    }

    private sealed record CaseFacts(
        string SourceCode,
        CasePriority Priority,
        CaseStatus Status,
        int FutureExposure,
        DateOnly? DueDate,
        DateOnly? ExceptionExpiresOn);
}
