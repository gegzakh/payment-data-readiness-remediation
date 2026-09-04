using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Application.Ingest;

public sealed record IngestionBatchDto(
    Guid Id,
    string SourceCode,
    string FileName,
    IngestionFormat Format,
    IngestionChannel Channel,
    long SizeBytes,
    string Checksum,
    string IdempotencyKey,
    string ParserVersion,
    string SubmittedBy,
    bool IsReprocess,
    BatchStatus Status,
    string? QuarantineReason,
    string? ErrorSummary,
    int RecordCount,
    int ParsedCount,
    int FailedCount,
    int DuplicateCount,
    int ExcludedCount,
    int Checkpoint,
    int RetryCount,
    bool CountsReconcile,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record PartyAddressRecordDto(
    Guid Id,
    Guid BatchId,
    int Sequence,
    string? MessageId,
    string? EndToEndId,
    PartyRole PartyRole,
    string? PartyName,
    string? Country,
    string? TownName,
    string? PostCode,
    string? StreetName,
    string? BuildingNumber,
    string? AddressLines,
    string? SchemeCode,
    string ContentHash,
    bool IsDuplicate);

/// <summary>Counts across ingested batches, for the operational overview (FR-ING-006).</summary>
public sealed record IngestionOverviewDto(
    int TotalBatches,
    int ParsedBatches,
    int QuarantinedBatches,
    int FailedBatches,
    int TotalRecords,
    int DuplicateRecords,
    DateTimeOffset AsOfUtc);

public static class IngestionSettingKeys
{
    public const string MaxFileBytes = "ingestion.file.max-bytes";
    public const string AllowedExtensions = "ingestion.file.allowed-extensions";
    public const string MaxRecords = "ingestion.file.max-records";
    public const string CsvDelimiter = "ingestion.csv.delimiter";
    public const string DefaultSchemeCode = "ingestion.default-scheme-code";
    public const string PageSize = "ingestion.page-size";
}
