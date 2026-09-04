namespace PDR.Remediation.Domain.Cases;

/// <summary>Where a case stands in the maker-checker workflow (FR-WF-005).</summary>
public enum CaseStatus
{
    New = 0,
    InProgress = 1,
    PendingApproval = 2,
    Approved = 3,
    Returned = 4,
    Rejected = 5,
    Dismissed = 6,
    ExceptionGranted = 7,
    WriteBackPending = 8,
    Remediated = 9,
    Failed = 10,
    RolledBack = 11
}

/// <summary>How a proposed correction was produced (FR-REM-003, FR-REM-004).</summary>
public enum ProposalMethod
{
    DeterministicParse = 0,
    ReferenceData = 1,
    SourceAttribute = 2,
    ManualEdit = 3,

    /// <summary>Machine assistance. Never authoritative on its own; a human must verify it (FR-REM-005).</summary>
    AssistedSuggestion = 4
}

public enum CasePriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>A checker's verdict on a submitted case (FR-WF-003).</summary>
public enum DecisionType
{
    Approve = 0,
    Return = 1,
    Reject = 2,
    Dismiss = 3,
    GrantException = 4
}

public enum PartyRole
{
    Debtor = 0,
    Creditor = 1,
    UltimateDebtor = 2,
    UltimateCreditor = 3,
    Unknown = 4
}

/// <summary>How a correction reaches the owning source system (FR-WB-001, FR-WB-004).</summary>
public enum WriteBackMode
{
    Api = 0,
    Export = 1
}

public enum WriteBackStatus
{
    Pending = 0,
    Applied = 1,
    Confirmed = 2,
    PartiallyFailed = 3,
    Failed = 4,
    RolledBack = 5
}

public enum WriteBackItemStatus
{
    Pending = 0,
    Applied = 1,
    Confirmed = 2,
    Failed = 3,
    Stale = 4,
    RolledBack = 5
}

public enum CampaignAudience
{
    InternalTeam = 0,
    CorporateCustomer = 1
}

public enum CampaignStatus
{
    Draft = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}
