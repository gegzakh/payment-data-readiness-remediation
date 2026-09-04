using Microsoft.EntityFrameworkCore;
using PDR.Reporting.Domain.Dashboards;

namespace PDR.Reporting.Application.Abstractions;

public interface IReportingDbContext
{
    DbSet<DashboardSnapshot> Snapshots { get; }

    DbSet<MetricValue> Metrics { get; }

    DbSet<MetricBreakdown> Breakdown { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
