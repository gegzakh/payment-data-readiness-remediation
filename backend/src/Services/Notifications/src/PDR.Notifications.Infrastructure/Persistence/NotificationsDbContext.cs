using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Persistence;
using PDR.Notifications.Application.Abstractions;
using PDR.Notifications.Domain.Schedules;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext(
    DbContextOptions<NotificationsDbContext> options,
    IAuditContext auditContext,
    ICorrelationContext correlationContext)
    : BaseDbContext(options, auditContext, correlationContext), INotificationsDbContext
{
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<Delivery> Deliveries => Set<Delivery>();

    public DbSet<ScheduledReport> ScheduledReports => Set<ScheduledReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
