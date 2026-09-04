using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Persistence;
using PDR.Remediation.Application.Abstractions;
using PDR.Remediation.Domain.Campaigns;
using PDR.Remediation.Domain.Cases;
using PDR.Remediation.Domain.WriteBack;

namespace PDR.Remediation.Infrastructure.Persistence;

public sealed class RemediationDbContext(
    DbContextOptions<RemediationDbContext> options,
    IAuditContext auditContext,
    ICorrelationContext correlationContext)
    : BaseDbContext(options, auditContext, correlationContext), IRemediationDbContext
{
    public DbSet<RemediationCase> Cases => Set<RemediationCase>();

    public DbSet<CaseEvent> CaseEvents => Set<CaseEvent>();

    public DbSet<CaseEvidence> CaseEvidence => Set<CaseEvidence>();

    public DbSet<Campaign> Campaigns => Set<Campaign>();

    public DbSet<WriteBackJob> WriteBackJobs => Set<WriteBackJob>();

    public DbSet<WriteBackItem> WriteBackItems => Set<WriteBackItem>();

    public DbSet<WriteBackTarget> WriteBackTargets => Set<WriteBackTarget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RemediationDbContext).Assembly);
    }
}
