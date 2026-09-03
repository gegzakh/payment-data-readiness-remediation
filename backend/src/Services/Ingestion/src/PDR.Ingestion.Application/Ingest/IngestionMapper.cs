using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Application.Ingest;

public static class IngestionMapper
{
    public static IngestionBatchDto ToDto(this IngestionBatch batch) =>
        new(
            batch.Id,
            batch.SourceCode,
            batch.FileName,
            batch.Format,
            batch.Channel,
            batch.SizeBytes,
            batch.Checksum,
            batch.IdempotencyKey,
            batch.ParserVersion,
            batch.SubmittedBy,
            batch.IsReprocess,
            batch.Status,
            batch.QuarantineReason,
            batch.ErrorSummary,
            batch.RecordCount,
            batch.ParsedCount,
            batch.FailedCount,
            batch.DuplicateCount,
            batch.ExcludedCount,
            batch.Checkpoint,
            batch.RetryCount,
            batch.CountsReconcile(),
            batch.ReceivedAtUtc,
            batch.StartedAtUtc,
            batch.CompletedAtUtc);

    /// <summary>
    /// Records are personal data, so callers without the drill-down permission only ever see a masked
    /// projection: enough to judge the address structure, not enough to identify the party (FR-VAL-009).
    /// </summary>
    public static PartyAddressRecordDto ToDto(this PartyAddressRecord record, bool unmasked) =>
        new(
            record.Id,
            record.BatchId,
            record.Sequence,
            record.MessageId,
            unmasked ? record.EndToEndId : Mask(record.EndToEndId),
            record.PartyRole,
            unmasked ? record.PartyName : Mask(record.PartyName),
            record.Country,
            unmasked ? record.TownName : Mask(record.TownName),
            unmasked ? record.PostCode : Mask(record.PostCode),
            unmasked ? record.StreetName : Mask(record.StreetName),
            unmasked ? record.BuildingNumber : Mask(record.BuildingNumber),
            unmasked ? record.AddressLines : Mask(record.AddressLines),
            record.SchemeCode,
            record.ContentHash,
            record.IsDuplicate);

    private static string? Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 2
            ? new string('*', trimmed.Length)
            : string.Concat(trimmed.AsSpan(0, 2), new string('*', Math.Min(trimmed.Length - 2, 8)));
    }
}
