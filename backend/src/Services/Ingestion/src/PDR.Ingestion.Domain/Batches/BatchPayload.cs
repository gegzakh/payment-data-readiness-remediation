using PDR.BuildingBlocks.Domain;

namespace PDR.Ingestion.Domain.Batches;

/// <summary>
/// The payload as received, kept apart from the batch header so listing batches never loads it. It is
/// what a retry re-reads (FR-ING-005) and what an evidence pointer ultimately resolves to (FR-VAL-003).
/// </summary>
public sealed class BatchPayload : Entity
{
    private BatchPayload()
    {
    }

    private BatchPayload(Guid batchId, byte[] content)
    {
        BatchId = batchId;
        Content = content;
    }

    public Guid BatchId { get; private set; }

    public byte[] Content { get; private set; } = [];

    public static BatchPayload Create(Guid batchId, byte[] content) => new(batchId, content);
}
