namespace PDR.Rules.Domain.Rulesets;

/// <summary>How badly a party record fails when the rule is not satisfied (FR-RUL-002).</summary>
public enum RuleSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

/// <summary>The deterministic check a rule performs against one address field.</summary>
public enum RuleKind
{
    Required = 0,
    MaxLength = 1,
    Pattern = 2,
    AllowedValues = 3,
    Prohibited = 4,
    StructuredOnly = 5
}

/// <summary>
/// Whether the rule belongs to the scheme validation in force today, the validation that starts at the
/// cutover date, or both. Validation evaluates "current" and "future" separately (FR-VAL-004).
/// </summary>
public enum RuleApplicability
{
    Current = 0,
    Future = 1,
    Both = 2
}

public enum RulesetStatus
{
    Draft = 0,
    Active = 1,
    Retired = 2
}
