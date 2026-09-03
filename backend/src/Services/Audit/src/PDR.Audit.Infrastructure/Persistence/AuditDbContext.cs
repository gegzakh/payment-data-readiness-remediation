using Microsoft.EntityFrameworkCore;
using PDR.Audit.Application.Abstractions;
using PDR.Audit.Domain.Ledger;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Persistence;

namespace PDR.Audit.Infrastructure.Persistence;

public sealed class AuditDbContext(
    DbContextOptions<AuditDbContext> options,
    IAuditContext auditContext,
    ICorrelationContext correlationContext)
    : BaseDbContext(options, auditContext, correlationContext), IAuditDbContext
{
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
    }
}
