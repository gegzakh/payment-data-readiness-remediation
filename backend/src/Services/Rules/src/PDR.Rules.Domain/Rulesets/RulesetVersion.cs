using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;

namespace PDR.Rules.Domain.Rulesets;

/// <summary>
/// An immutable-once-activated snapshot of a scheme's rules. Validation always evaluates against the
/// version that was effective at a given point in time, which is what makes results reproducible.
/// </summary>
public sealed class RulesetVersion : Entity
{
    private readonly List<RuleDefinition> _rules = [];

    private RulesetVersion()
    {
    }

    internal RulesetVersion(Guid rulesetId, int versionNumber, string? notes)
    {
        RulesetId = rulesetId;
        VersionNumber = versionNumber;
        Notes = notes;
        Status = RulesetStatus.Draft;
    }

    public Guid RulesetId { get; private set; }

    public int VersionNumber { get; private set; }

    public RulesetStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public DateOnly? EffectiveFrom { get; private set; }

    public DateOnly? EffectiveTo { get; private set; }

    public DateTimeOffset? ActivatedAtUtc { get; private set; }

    public string? ActivatedBy { get; private set; }

    public IReadOnlyList<RuleDefinition> Rules => _rules;

    internal RuleDefinition AddRule(
        string code,
        string field,
        RuleKind kind,
        RuleSeverity severity,
        RuleApplicability applicability,
        string message,
        string? parameter)
    {
        var rule = new RuleDefinition(Id, code, field, kind, severity, applicability, message, parameter);
        _rules.Add(rule);
        return rule;
    }

    internal bool RemoveRule(Guid ruleId)
    {
        var rule = _rules.Find(r => r.Id == ruleId);
        return rule is not null && _rules.Remove(rule);
    }

    internal void CopyRulesFrom(RulesetVersion source)
    {
        foreach (var rule in source.Rules)
        {
            AddRule(rule.Code, rule.Field, rule.Kind, rule.Severity, rule.Applicability, rule.Message, rule.Parameter);
        }
    }

    internal void Activate(DateOnly effectiveFrom, string actor, DateTimeOffset activatedAtUtc)
    {
        Status = RulesetStatus.Active;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = null;
        ActivatedBy = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(actor), 256);
        ActivatedAtUtc = activatedAtUtc;
    }

    internal void Retire(DateOnly effectiveTo)
    {
        Status = RulesetStatus.Retired;
        EffectiveTo = effectiveTo;
    }

    internal bool HasRuleCode(string code) =>
        _rules.Exists(rule => string.Equals(rule.Code, code, StringComparison.OrdinalIgnoreCase));
}
