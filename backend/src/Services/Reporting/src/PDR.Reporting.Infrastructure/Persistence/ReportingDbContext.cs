using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Persistence;
using PDR.Reporting.Application.Abstractions;
using PDR.Reporting.Domain.Dashboards;

namespace PDR.Reporting.Infrastructure.Persistence;

public sealed class ReportingDbContext(
    DbContextOptions<ReportingDbContext> options,
    IAuditContext auditContext,
    ICorrelationContext correlationContext)
    : BaseDbContext(options, auditContext, correlationContext), IReportingDbContext
{
    public DbSet<DashboardSnapshot> Snapshots => Set<DashboardSnapshot>();

    public DbSet<MetricValue> Metrics => Set<MetricValue>();

    public DbSet<MetricBreakdown> Breakdown => Set<MetricBreakdown>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportingDbContext).Assembly);
    }
}
