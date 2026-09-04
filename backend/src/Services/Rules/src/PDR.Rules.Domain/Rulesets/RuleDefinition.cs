using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;

namespace PDR.Rules.Domain.Rulesets;

/// <summary>
/// One deterministic check inside a ruleset version: "field X must satisfy Y, otherwise Z".
/// Rules are data so schemes can be changed without shipping code (FR-RUL-001).
/// </summary>
public sealed class RuleDefinition : Entity
{
    private RuleDefinition()
    {
    }

    internal RuleDefinition(
        Guid rulesetVersionId,
        string code,
        string field,
        RuleKind kind,
        RuleSeverity severity,
        RuleApplicability applicability,
        string message,
        string? parameter)
    {
        RulesetVersionId = rulesetVersionId;
        Code = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(code), 64);
        Field = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(field), 64);
        Kind = kind;
        Severity = severity;
        Applicability = applicability;
        Message = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(message), 512);
        Parameter = parameter;
    }

    public Guid RulesetVersionId { get; private set; }

    /// <summary>Stable identifier quoted on issues and in remediation guidance, e.g. "ADDR.TOWN_REQUIRED".</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Party-address field the rule inspects, e.g. "TownName" or "Country".</summary>
    public string Field { get; private set; } = string.Empty;

    public RuleKind Kind { get; private set; }

    public RuleSeverity Severity { get; private set; }

    public RuleApplicability Applicability { get; private set; }

    public string Message { get; private set; } = string.Empty;

    /// <summary>Kind-specific argument: the length for MaxLength, the regex for Pattern, a CSV for AllowedValues.</summary>
    public string? Parameter { get; private set; }
}
