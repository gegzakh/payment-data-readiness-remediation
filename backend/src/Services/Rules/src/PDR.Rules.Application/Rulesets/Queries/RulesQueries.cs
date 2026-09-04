using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;
using PDR.Rules.Application.Abstractions;
using PDR.Rules.Domain.Rulesets;

namespace PDR.Rules.Application.Rulesets.Queries;

public sealed record GetSchemesQuery(bool IncludeInactive = false) : IQuery<IReadOnlyList<SchemeDto>>;

public sealed record GetCountriesQuery(bool SepaOnly = false) : IQuery<IReadOnlyList<CountryDto>>;

public sealed record GetRulesetsQuery(string? SchemeCode = null) : IQuery<IReadOnlyList<RulesetDto>>;

public sealed record GetRulesetByIdQuery(Guid Id) : IQuery<RulesetDto>;

/// <summary>
/// The rules effective for a scheme on a date. <paramref name="Mode"/> selects today's scheme validation
/// or the post-cutover validation, which is how "current vs future" readiness is produced.
/// </summary>
public sealed record GetEffectiveRulesQuery(
    string SchemeCode,
    DateOnly? AsOf = null,
    RuleApplicability Mode = RuleApplicability.Current) : IQuery<EffectiveRulesetDto>;

public sealed class GetSchemesQueryHandler(IRulesDbContext context)
    : IRequestHandler<GetSchemesQuery, Result<IReadOnlyList<SchemeDto>>>
{
    public async Task<Result<IReadOnlyList<SchemeDto>>> HandleAsync(
        GetSchemesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Schemes.AsNoTracking().AsQueryable();
        if (!request.IncludeInactive)
        {
            query = query.Where(scheme => scheme.IsActive);
        }

        var schemes = await query.OrderBy(scheme => scheme.Code).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<SchemeDto>>(schemes.Select(RulesMapping.ToDto).ToList());
    }
}

public sealed class GetCountriesQueryHandler(IRulesDbContext context)
    : IRequestHandler<GetCountriesQuery, Result<IReadOnlyList<CountryDto>>>
{
    public async Task<Result<IReadOnlyList<CountryDto>>> HandleAsync(
        GetCountriesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Countries.AsNoTracking().AsQueryable();
        if (request.SepaOnly)
        {
            query = query.Where(country => country.IsSepa);
        }

        var countries = await query.OrderBy(country => country.Alpha2).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CountryDto>>(countries.Select(RulesMapping.ToDto).ToList());
    }
}

public sealed class GetRulesetsQueryHandler(IRulesDbContext context)
    : IRequestHandler<GetRulesetsQuery, Result<IReadOnlyList<RulesetDto>>>
{
    public async Task<Result<IReadOnlyList<RulesetDto>>> HandleAsync(
        GetRulesetsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Rulesets
            .AsNoTracking()
            .Include(ruleset => ruleset.Versions)
            .ThenInclude(version => version.Rules)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SchemeCode))
        {
            var schemeCode = request.SchemeCode.ToUpperInvariant();
            query = query.Where(ruleset => ruleset.SchemeCode == schemeCode);
        }

        var rulesets = await query.OrderBy(ruleset => ruleset.SchemeCode).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<RulesetDto>>(rulesets.Select(RulesMapping.ToDto).ToList());
    }
}

public sealed class GetRulesetByIdQueryHandler(IRulesDbContext context)
    : IRequestHandler<GetRulesetByIdQuery, Result<RulesetDto>>
{
    public async Task<Result<RulesetDto>> HandleAsync(
        GetRulesetByIdQuery request,
        CancellationToken cancellationToken)
    {
        var ruleset = await context.Rulesets
            .AsNoTracking()
            .Include(entity => entity.Versions)
            .ThenInclude(version => version.Rules)
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken);

        return ruleset is null
            ? Result.Failure<RulesetDto>(RulesetErrors.NotFound(request.Id))
            : ruleset.ToDto();
    }
}

public sealed class GetEffectiveRulesQueryHandler(IRulesDbContext context, IClock clock)
    : IRequestHandler<GetEffectiveRulesQuery, Result<EffectiveRulesetDto>>
{
    public async Task<Result<EffectiveRulesetDto>> HandleAsync(
        GetEffectiveRulesQuery request,
        CancellationToken cancellationToken)
    {
        var schemeCode = request.SchemeCode.ToUpperInvariant();
        var asOf = request.AsOf ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        var ruleset = await context.Rulesets
            .AsNoTracking()
            .Include(entity => entity.Versions)
            .ThenInclude(version => version.Rules)
            .FirstOrDefaultAsync(entity => entity.SchemeCode == schemeCode, cancellationToken);

        if (ruleset is null)
        {
            return Result.Failure<EffectiveRulesetDto>(RulesetErrors.SchemeNotFound(request.SchemeCode));
        }

        var version = ruleset.Versions
            .Where(candidate => candidate.EffectiveFrom is not null && candidate.EffectiveFrom <= asOf)
            .Where(candidate => candidate.EffectiveTo is null || candidate.EffectiveTo > asOf)
            .OrderByDescending(candidate => candidate.EffectiveFrom)
            .ThenByDescending(candidate => candidate.VersionNumber)
            .FirstOrDefault();

        if (version is null)
        {
            return Result.Failure<EffectiveRulesetDto>(RulesetErrors.NoActiveRuleset(schemeCode, asOf));
        }

        var rules = version.Rules
            .Where(rule => rule.Applicability == RuleApplicability.Both || rule.Applicability == request.Mode)
            .OrderBy(rule => rule.Code, StringComparer.Ordinal)
            .Select(RulesMapping.ToDto)
            .ToList();

        return new EffectiveRulesetDto(
            ruleset.SchemeCode,
            ruleset.Id,
            version.VersionNumber,
            version.EffectiveFrom,
            asOf,
            request.Mode,
            rules);
    }
}
