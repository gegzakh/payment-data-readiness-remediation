namespace PDR.Ingestion.Domain.Batches;

/// <summary>Supported input layouts (FR-ING-002).</summary>
public enum IngestionFormat
{
    Iso20022Xml = 0,
    Csv = 1
}

/// <summary>How the payload reached the platform (FR-ING-001).</summary>
public enum IngestionChannel
{
    Upload = 0,
    Api = 1,
    Sftp = 2,
    ObjectStorage = 3,
    Event = 4,
    Database = 5
}

/// <summary>Batch lifecycle (FR-ING-004, FR-ING-005).</summary>
public enum BatchStatus
{
    Received = 0,
    Quarantined = 1,
    Parsing = 2,
    Parsed = 3,
    Failed = 4,
    Cancelled = 5
}

/// <summary>Party whose postal address the record carries.</summary>
public enum PartyRole
{
    Debtor = 0,
    Creditor = 1,
    UltimateDebtor = 2,
    UltimateCreditor = 3
}
