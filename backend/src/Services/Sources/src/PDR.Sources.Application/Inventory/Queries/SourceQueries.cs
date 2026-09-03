using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;
using PDR.Sources.Application.Abstractions;
using PDR.Sources.Domain.Inventory;

namespace PDR.Sources.Application.Inventory.Queries;

public sealed record GetSourcesQuery(
    string? SchemeCode = null,
    OnboardingStatus? Status = null,
    string? LegalEntity = null,
    bool AttestationOverdueOnly = false,
    bool IncludeInactive = false) : IQuery<IReadOnlyList<SourceSystemDto>>;

public sealed record GetSourceByCodeQuery(string Code) : IQuery<SourceSystemDto>;

public sealed record GetSourceReadinessQuery(string? SchemeCode = null) : IQuery<SourceReadinessSummaryDto>;

public sealed class GetSourceByCodeQueryValidator : AbstractValidator<GetSourceByCodeQuery>
{
    public GetSourceByCodeQueryValidator() => RuleFor(query => query.Code).NotEmpty().MaximumLength(32);
}

public sealed class GetSourcesQueryHandler(
    ISourcesDbContext context,
    SourceReadinessPolicy policy,
    IClock clock) : IRequestHandler<GetSourcesQuery, Result<IReadOnlyList<SourceSystemDto>>>
{
    public async Task<Result<IReadOnlyList<SourceSystemDto>>> HandleAsync(
        GetSourcesQuery request,
        CancellationToken cancellationToken)
    {
        var sources = await context.LoadAsync(request.IncludeInactive, request.Status, request.LegalEntity, cancellationToken);
        var (attestationDays, freshnessDays) = await policy.ResolveAsync(cancellationToken);
        var now = clock.UtcNow;

        var items = sources
            .Where(source => MatchesScheme(source, request.SchemeCode))
            .Where(source => !request.AttestationOverdueOnly || source.IsAttestationOverdue(now, attestationDays))
            .Select(source => source.ToDto(now, attestationDays, freshnessDays))
            .OrderBy(source => source.ReadinessScore)
            .ThenBy(source => source.Code, StringComparer.Ordinal)
            .ToList();

        return Result.Success<IReadOnlyList<SourceSystemDto>>(items);
    }

    internal static bool MatchesScheme(SourceSystem source, string? schemeCode) =>
        string.IsNullOrWhiteSpace(schemeCode) ||
        SourceMapper.SplitSchemes(source.SchemeCodes)
            .Contains(schemeCode.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);
}

public sealed class GetSourceByCodeQueryHandler(
    ISourcesDbContext context,
    SourceReadinessPolicy policy,
    IClock clock) : IRequestHandler<GetSourceByCodeQuery, Result<SourceSystemDto>>
{
    public async Task<Result<SourceSystemDto>> HandleAsync(
        GetSourceByCodeQuery request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();

        var source = await context.SourceSystems
            .AsNoTracking()
            .Include(entity => entity.Mappings)
            .Include(entity => entity.Lineage)
            .FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);

        if (source is null)
        {
            return Result.Failure<SourceSystemDto>(SourceErrors.NotFound(request.Code));
        }

        var (attestationDays, freshnessDays) = await policy.ResolveAsync(cancellationToken);
        return source.ToDto(clock.UtcNow, attestationDays, freshnessDays);
    }
}

public sealed class GetSourceReadinessQueryHandler(
    ISourcesDbContext context,
    SourceReadinessPolicy policy,
    IClock clock) : IRequestHandler<GetSourceReadinessQuery, Result<SourceReadinessSummaryDto>>
{
    public async Task<Result<SourceReadinessSummaryDto>> HandleAsync(
        GetSourceReadinessQuery request,
        CancellationToken cancellationToken)
    {
        var sources = await context.LoadAsync(false, null, null, cancellationToken);
        var (attestationDays, freshnessDays) = await policy.ResolveAsync(cancellationToken);
        var now = clock.UtcNow;

        var scoped = sources
            .Where(source => GetSourcesQueryHandler.MatchesScheme(source, request.SchemeCode))
            .ToList();

        if (scoped.Count == 0)
        {
            return new SourceReadinessSummaryDto(0, 0, 0, 0, 0, 0, 0, now);
        }

        var covered = scoped.Sum(source =>
            (long)Math.Round(source.EstimatedPartyCount * (source.ScanCoveragePercent / 100m)));

        return new SourceReadinessSummaryDto(
            scoped.Count,
            scoped.Count(source => source.Status == OnboardingStatus.Ready),
            scoped.Count(source => source.Status == OnboardingStatus.Blocked),
            scoped.Count(source => source.IsAttestationOverdue(now, attestationDays)),
            covered,
            scoped.Sum(source => source.EstimatedPartyCount),
            Math.Round(scoped.Average(source => source.ReadinessScore(now, attestationDays, freshnessDays)), 2),
            now);
    }
}

internal static class SourceQueryExtensions
{
    public static async Task<IReadOnlyList<SourceSystem>> LoadAsync(
        this ISourcesDbContext context,
        bool includeInactive,
        OnboardingStatus? status,
        string? legalEntity,
        CancellationToken cancellationToken)
    {
        var query = context.SourceSystems
            .AsNoTracking()
            .Include(source => source.Mappings)
            .Include(source => source.Lineage)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(source => source.IsActive);
        }

        if (status.HasValue)
        {
            query = query.Where(source => source.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(legalEntity))
        {
            query = query.Where(source => source.LegalEntity == legalEntity);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
