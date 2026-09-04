using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;

namespace PDR.Ingestion.Domain.Batches;

/// <summary>
/// A single ingestion run over one payload. It carries the full provenance the programme has to be
/// able to show later — source, file, checksum, parser version, counts, timestamps and why anything
/// was quarantined or excluded (FR-ING-006).
/// </summary>
public sealed class IngestionBatch : AggregateRoot
{
    private IngestionBatch()
    {
    }

#pragma warning disable S107 // Provenance fields are captured together at creation on purpose.
    private IngestionBatch(
        string sourceCode,
        string fileName,
        IngestionFormat format,
        IngestionChannel channel,
        long sizeBytes,
        string checksum,
        string idempotencyKey,
        string parserVersion,
        string submittedBy,
        bool isReprocess,
        DateTimeOffset receivedAtUtc)
    {
        SourceCode = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(sourceCode), 32).ToUpperInvariant();
        FileName = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(fileName), 256);
        Format = format;
        Channel = channel;
        SizeBytes = sizeBytes;
        Checksum = Ensure.NotNullOrWhiteSpace(checksum);
        IdempotencyKey = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(idempotencyKey), 128);
        ParserVersion = parserVersion;
        SubmittedBy = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(submittedBy), 128);
        IsReprocess = isReprocess;
        ReceivedAtUtc = receivedAtUtc;
        Status = BatchStatus.Received;
    }
#pragma warning restore S107

    public string SourceCode { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public IngestionFormat Format { get; private set; }

    public IngestionChannel Channel { get; private set; }

    public long SizeBytes { get; private set; }

    /// <summary>SHA-256 of the payload; also the duplicate-scan key (FR-ING-007).</summary>
    public string Checksum { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    public string ParserVersion { get; private set; } = string.Empty;

    public string SubmittedBy { get; private set; } = string.Empty;

    /// <summary>Set when the same payload is deliberately scanned again (FR-ING-007).</summary>
    public bool IsReprocess { get; private set; }

    public BatchStatus Status { get; private set; }

    public string? QuarantineReason { get; private set; }

    public string? ErrorSummary { get; private set; }

    public int RecordCount { get; private set; }

    public int ParsedCount { get; private set; }

    public int FailedCount { get; private set; }

    public int DuplicateCount { get; private set; }

    public int ExcludedCount { get; private set; }

    /// <summary>Last record index durably processed, so a retry resumes instead of restarting (FR-ING-005).</summary>
    public int Checkpoint { get; private set; }

    public int RetryCount { get; private set; }

    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

#pragma warning disable S107
    public static IngestionBatch Receive(
        string sourceCode,
        string fileName,
        IngestionFormat format,
        IngestionChannel channel,
        long sizeBytes,
        string checksum,
        string idempotencyKey,
        string parserVersion,
        string submittedBy,
        bool isReprocess,
        DateTimeOffset receivedAtUtc) =>
        new(
            sourceCode,
            fileName,
            format,
            channel,
            sizeBytes,
            checksum,
            idempotencyKey,
            parserVersion,
            submittedBy,
            isReprocess,
            receivedAtUtc);
#pragma warning restore S107

    /// <summary>Rejected before any parsing; the payload is never processed (FR-ING-004).</summary>
    public void Quarantine(string reason, DateTimeOffset atUtc)
    {
        Status = BatchStatus.Quarantined;
        QuarantineReason = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(reason), 512);
        CompletedAtUtc = atUtc;
    }

    public Result StartParsing(DateTimeOffset atUtc)
    {
        if (Status is BatchStatus.Quarantined or BatchStatus.Cancelled)
        {
            return Result.Failure(BatchErrors.NotProcessable(Status));
        }

        Status = BatchStatus.Parsing;
        StartedAtUtc = atUtc;
        return Result.Success();
    }

    public void RecordCheckpoint(int recordIndex) => Checkpoint = Math.Max(Checkpoint, recordIndex);

    public void CompleteParsing(
        int recordCount,
        int parsedCount,
        int failedCount,
        int duplicateCount,
        int excludedCount,
        DateTimeOffset atUtc)
    {
        RecordCount = recordCount;
        ParsedCount = parsedCount;
        FailedCount = failedCount;
        DuplicateCount = duplicateCount;
        ExcludedCount = excludedCount;
        Checkpoint = recordCount;
        Status = BatchStatus.Parsed;
        CompletedAtUtc = atUtc;
    }

    public void Fail(string errorSummary, DateTimeOffset atUtc)
    {
        Status = BatchStatus.Failed;
        ErrorSummary = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(errorSummary), 1024);
        CompletedAtUtc = atUtc;
    }

    public Result Cancel(DateTimeOffset atUtc)
    {
        if (Status is BatchStatus.Parsed)
        {
            return Result.Failure(BatchErrors.AlreadyParsed);
        }

        Status = BatchStatus.Cancelled;
        CompletedAtUtc = atUtc;
        return Result.Success();
    }

    /// <summary>Only a failed batch may be retried, and it resumes from its checkpoint.</summary>
    public Result PrepareRetry(DateTimeOffset atUtc)
    {
        if (Status is not BatchStatus.Failed)
        {
            return Result.Failure(BatchErrors.NotRetryable(Status));
        }

        RetryCount++;
        ErrorSummary = null;
        Status = BatchStatus.Parsing;
        StartedAtUtc = atUtc;
        CompletedAtUtc = null;
        return Result.Success();
    }

    /// <summary>Counts must add up to the number of input records (FR-VAL-008).</summary>
    public bool CountsReconcile() =>
        Status is not BatchStatus.Parsed || RecordCount == ParsedCount + FailedCount + ExcludedCount;
}

public static class BatchErrors
{
    public static readonly Error AlreadyParsed =
        Error.Conflict("BATCH.ALREADY_PARSED", "A parsed batch cannot be cancelled.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("BATCH.NOT_FOUND", $"Ingestion batch '{id}' was not found.");

    public static Error NotProcessable(BatchStatus status) =>
        Error.Conflict("BATCH.NOT_PROCESSABLE", $"A batch in status '{status}' cannot be parsed.");

    public static Error NotRetryable(BatchStatus status) =>
        Error.Conflict("BATCH.NOT_RETRYABLE", $"Only failed batches can be retried; this one is '{status}'.");

    public static Error DuplicateScan(Guid existingBatchId) =>
        Error.Conflict(
            "BATCH.DUPLICATE_SCAN",
            $"This payload was already ingested as batch '{existingBatchId}'. Resubmit with reprocess=true to scan it again.");

    public static Error Rejected(string reason) =>
        Error.Validation("BATCH.REJECTED", reason);
}
