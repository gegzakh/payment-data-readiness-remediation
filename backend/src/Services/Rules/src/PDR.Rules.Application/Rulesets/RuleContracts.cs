using PDR.Rules.Domain.Reference;
using PDR.Rules.Domain.Rulesets;
using PDR.Rules.Domain.Schemes;

namespace PDR.Rules.Application.Rulesets;

public sealed record SchemeDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    DateOnly? StructuredAddressMandatoryFrom,
    bool IsActive);

public sealed record RuleDto(
    Guid Id,
    string Code,
    string Field,
    RuleKind Kind,
    RuleSeverity Severity,
    RuleApplicability Applicability,
    string Message,
    string? Parameter);

public sealed record RulesetVersionDto(
    Guid Id,
    int VersionNumber,
    RulesetStatus Status,
    string? Notes,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    DateTimeOffset? ActivatedAtUtc,
    string? ActivatedBy,
    IReadOnlyList<RuleDto> Rules);

public sealed record RulesetDto(
    Guid Id,
    string SchemeCode,
    string Name,
    string? Description,
    int? ActiveVersionNumber,
    IReadOnlyList<RulesetVersionDto> Versions);

/// <summary>
/// The contract other services (Validation, Simulation) consume: the rules that apply to a scheme at a
/// point in time, already filtered to current or future applicability.
/// </summary>
public sealed record EffectiveRulesetDto(
    string SchemeCode,
    Guid RulesetId,
    int VersionNumber,
    DateOnly? EffectiveFrom,
    DateOnly AsOf,
    RuleApplicability Mode,
    IReadOnlyList<RuleDto> Rules);

public sealed record CountryDto(string Alpha2, string Name, bool RequiresPostCode, bool IsSepa);

public sealed record RuleInput(
    string Code,
    string Field,
    RuleKind Kind,
    RuleSeverity Severity,
    RuleApplicability Applicability,
    string Message,
    string? Parameter);

public static class RulesMapping
{
    public static SchemeDto ToDto(this Scheme scheme) =>
        new(scheme.Id, scheme.Code, scheme.Name, scheme.Description, scheme.StructuredAddressMandatoryFrom, scheme.IsActive);

    public static CountryDto ToDto(this Country country) =>
        new(country.Alpha2, country.Name, country.RequiresPostCode, country.IsSepa);

    public static RuleDto ToDto(this RuleDefinition rule) =>
        new(rule.Id, rule.Code, rule.Field, rule.Kind, rule.Severity, rule.Applicability, rule.Message, rule.Parameter);

    public static RulesetVersionDto ToDto(this RulesetVersion version) =>
        new(
            version.Id,
            version.VersionNumber,
            version.Status,
            version.Notes,
            version.EffectiveFrom,
            version.EffectiveTo,
            version.ActivatedAtUtc,
            version.ActivatedBy,
            version.Rules.OrderBy(rule => rule.Code, StringComparer.Ordinal).Select(ToDto).ToList());

    public static RulesetDto ToDto(this Ruleset ruleset) =>
        new(
            ruleset.Id,
            ruleset.SchemeCode,
            ruleset.Name,
            ruleset.Description,
            ruleset.ActiveVersion?.VersionNumber,
            ruleset.Versions.OrderByDescending(version => version.VersionNumber).Select(ToDto).ToList());
}
