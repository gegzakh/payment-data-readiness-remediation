using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PDR.BuildingBlocks.Core.Time;
using PDR.Notifications.Application.Abstractions;
using PDR.Notifications.Application.Notifications;
using PDR.Notifications.Application.Schedules;

namespace PDR.Notifications.Infrastructure.Workers;

public sealed class NotificationWorkerOptions
{
    public const string SectionName = "Worker";

    public bool Enabled { get; set; } = true;

    public int PollSeconds { get; set; } = 15;
}

/// <summary>
/// Drains due deliveries and fires due scheduled reports on a fixed cadence. Everything it does is also
/// reachable through the operator endpoints, so tests and incident response never depend on the timer;
/// disabling the worker leaves the service fully functional, just manually driven.
/// </summary>
public sealed class NotificationWorker(
    IServiceScopeFactory scopeFactory,
    NotificationWorkerOptions options,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("Notification worker is disabled; deliveries only run on demand.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(options.PollSeconds, 1)));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var reports = scope.ServiceProvider.GetRequiredService<ScheduledReportRunner>();
                var context = scope.ServiceProvider.GetRequiredService<INotificationsDbContext>();
                var now = scope.ServiceProvider.GetRequiredService<IClock>().UtcNow;

                var due = await context.ScheduledReports
                    .Where(report => report.IsEnabled && report.NextRunAtUtc != null && report.NextRunAtUtc <= now)
                    .ToListAsync(stoppingToken);

                await reports.RunAsync(due, stoppingToken);

                var dispatcher = scope.ServiceProvider.GetRequiredService<DeliveryDispatcher>();
                var summary = await dispatcher.DispatchDueAsync(stoppingToken);

                if (summary.Attempted > 0)
                {
                    logger.LogInformation(
                        "Dispatched {Attempted} deliveries: {Delivered} delivered, {Retrying} retrying, {DeadLettered} dead-lettered.",
                        summary.Attempted,
                        summary.Delivered,
                        summary.Retrying,
                        summary.DeadLettered);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The notification worker pass failed; retrying on the next tick.");
            }
        }
    }
}
