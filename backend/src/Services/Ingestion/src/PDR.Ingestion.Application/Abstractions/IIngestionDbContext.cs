using Microsoft.EntityFrameworkCore;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Application.Abstractions;

public interface IIngestionDbContext
{
    DbSet<IngestionBatch> Batches { get; }

    DbSet<PartyAddressRecord> Records { get; }

    DbSet<BatchPayload> Payloads { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
