using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Persistence;
using PDR.Ingestion.Application.Abstractions;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Infrastructure.Persistence;

public sealed class IngestionDbContext(
    DbContextOptions<IngestionDbContext> options,
    IAuditContext auditContext,
    ICorrelationContext correlationContext)
    : BaseDbContext(options, auditContext, correlationContext), IIngestionDbContext
{
    public DbSet<IngestionBatch> Batches => Set<IngestionBatch>();

    public DbSet<PartyAddressRecord> Records => Set<PartyAddressRecord>();

    public DbSet<BatchPayload> Payloads => Set<BatchPayload>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IngestionDbContext).Assembly);
    }
}
