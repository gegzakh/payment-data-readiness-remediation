using Microsoft.EntityFrameworkCore;
using PDR.Notifications.Domain.Schedules;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.Application.Abstractions;

public interface INotificationsDbContext
{
    DbSet<Subscription> Subscriptions { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<Delivery> Deliveries { get; }

    DbSet<ScheduledReport> ScheduledReports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Result of pushing one payload at one target; the transport detail stays in infrastructure.</summary>
public sealed record ChannelResult(bool Succeeded, int? StatusCode, string? Signature, string? Error)
{
    public static ChannelResult Success(int? statusCode = null, string? signature = null) =>
        new(true, statusCode, signature, null);

    public static ChannelResult Failure(string error, int? statusCode = null) =>
        new(false, statusCode, null, error);
}

public interface IChannelSender
{
    Domain.Subscriptions.DeliveryChannel Channel { get; }

    Task<ChannelResult> SendAsync(
        Subscription subscription,
        Notification notification,
        CancellationToken cancellationToken);
}
