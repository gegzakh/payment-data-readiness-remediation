using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDR.BuildingBlocks.Core.Time;
using PDR.Ingestion.Application.Abstractions;
using PDR.Ingestion.Application.Parsing;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Application.Ingest;

/// <summary>
/// Turns a received payload into party address records. Parsing is checkpointed and idempotent per
/// batch: records already written for the batch are dropped first, so a retry produces the same result
/// as a first run (FR-ING-005). Duplicates inside a batch are marked rather than dropped, because the
/// reconciliation has to account for every input record (FR-VAL-008).
/// </summary>
public sealed class BatchProcessor(
    IIngestionDbContext context,
    IEnumerable<IAddressParser> parsers,
    IClock clock,
    ILogger<BatchProcessor> logger)
{
    private const int CheckpointInterval = 500;

    public async Task ProcessAsync(
        IngestionBatch batch,
        byte[] content,
        FileSafetyOptions options,
        string? defaultSchemeCode,
        CancellationToken cancellationToken)
    {
        var parser = parsers.Single(candidate => candidate.Format == batch.Format);

        await context.Records
            .Where(record => record.BatchId == batch.Id)
            .ExecuteDeleteAsync(cancellationToken);

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            var outcome = parser.Parse(stream, new ParserOptions(options.CsvDelimiter, options.MaxRecords));

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var duplicates = 0;
            var sequence = 0;

            foreach (var address in outcome.Addresses)
            {
                sequence++;
                var record = PartyAddressRecord.Create(
                    batch.Id,
                    sequence,
                    address.MessageId,
                    address.EndToEndId,
                    address.PartyRole,
                    address.PartyName,
                    address.Country,
                    address.TownName,
                    address.PostCode,
                    address.StreetName,
                    address.BuildingNumber,
                    address.AddressLines,
                    defaultSchemeCode);

                if (!seen.Add(record.ContentHash))
                {
                    record.MarkDuplicate();
                    duplicates++;
                }

                context.Records.Add(record);

                if (sequence % CheckpointInterval == 0)
                {
                    batch.RecordCheckpoint(sequence);
                    await context.SaveChangesAsync(cancellationToken);
                }
            }

            batch.CompleteParsing(
                outcome.InputRecordCount,
                outcome.Addresses.Count,
                outcome.Failures.Count,
                duplicates,
                outcome.ExcludedCount,
                clock.UtcNow);

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (ParserException exception)
        {
            logger.LogWarning("Batch {BatchId} could not be parsed: {Reason}", batch.Id, exception.Message);
            batch.Fail(exception.Message, clock.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
