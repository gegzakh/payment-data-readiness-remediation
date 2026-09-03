using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Persistence;
using PDR.Sources.Application.Abstractions;
using PDR.Sources.Domain.Inventory;

namespace PDR.Sources.Infrastructure.Persistence;

public sealed class SourcesDbContext(
    DbContextOptions<SourcesDbContext> options,
    IAuditContext auditContext,
    ICorrelationContext correlationContext)
    : BaseDbContext(options, auditContext, correlationContext), ISourcesDbContext
{
    public DbSet<SourceSystem> SourceSystems => Set<SourceSystem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SourcesDbContext).Assembly);
    }
}
