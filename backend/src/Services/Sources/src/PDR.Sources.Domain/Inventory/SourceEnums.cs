namespace PDR.Sources.Domain.Inventory;

/// <summary>What kind of system produces the party/address data (FR-SRC-001).</summary>
public enum SourceKind
{
    PaymentHub = 0,
    Erp = 1,
    Crm = 2,
    MasterData = 3,
    FileFeed = 4,
    Channel = 5
}

/// <summary>How the platform receives data from the source (FR-ING-001).</summary>
public enum InterfaceKind
{
    Api = 0,
    Sftp = 1,
    Database = 2,
    Upload = 3,
    Event = 4
}

/// <summary>Where the source is in its onboarding journey (FR-SRC-005).</summary>
public enum OnboardingStatus
{
    Registered = 0,
    Onboarding = 1,
    Scanning = 2,
    Ready = 3,
    Blocked = 4
}

/// <summary>Maturity of the source-to-ISO 20022 field mapping (FR-SRC-003, FR-SRC-005).</summary>
public enum MappingReadiness
{
    NotStarted = 0,
    InProgress = 1,
    Ready = 2,
    NeedsRework = 3
}
