namespace PDR.Validation.Domain.Assessments;

/// <summary>How a party address is expressed, independent of whether it passes any rule (FR-VAL-003).</summary>
public enum AddressClassification
{
    /// <summary>Country plus discrete town/post code/street elements, no free-text lines.</summary>
    Structured = 0,

    /// <summary>Some discrete elements alongside free-text address lines.</summary>
    Hybrid = 1,

    /// <summary>Free-text address lines only.</summary>
    Unstructured = 2,

    /// <summary>No address content at all.</summary>
    Absent = 3,

    /// <summary>Content is present but cannot be interpreted as an address.</summary>
    Unrecognized = 4
}

/// <summary>Party whose address is being assessed.</summary>
public enum PartyRole
{
    Debtor = 0,
    Creditor = 1,
    UltimateDebtor = 2,
    UltimateCreditor = 3
}

/// <summary>Which rule set produced a finding: today's validation or the post-cutover one (FR-VAL-004).</summary>
public enum RuleMode
{
    Current = 0,
    Future = 1
}

public enum IssueSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

/// <summary>
/// The verdict for one party record under one rule mode. Rejected/warning/informational mirror rule
/// severity; excluded and unable-to-assess are kept distinct so reconciliation stays honest (FR-VAL-007).
/// </summary>
public enum RecordOutcome
{
    Compliant = 0,
    Informational = 1,
    Warning = 2,
    Rejected = 3,
    Excluded = 4,
    UnableToAssess = 5
}

public enum ValidationRunStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2
}

/// <summary>Dimensions the portfolio can be profiled by (FR-VAL-006).</summary>
public enum ProfileDimension
{
    Scheme = 0,
    Source = 1,
    PartyRole = 2,
    Country = 3,
    Classification = 4,
    Issue = 5
}
