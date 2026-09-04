using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.Notifications.Application.Abstractions;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.Application.Notifications;

/// <summary>
/// Drains the due deliveries. Runs both from the background worker and from an operator-triggered endpoint,
/// so a stuck queue can be pushed along without a restart. Each attempt is recorded on the delivery itself:
/// success, a backed-off retry, or a dead letter once the budget is spent (FR-API-002).
/// </summary>
public sealed class DeliveryDispatcher(
    INotificationsDbContext context,
    IEnumerable<IChannelSender> senders,
    ISettingsReader settings,
    IClock clock,
    ILogger<DeliveryDispatcher> logger)
{
    public async Task<DispatchSummaryDto> DispatchDueAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var batchSize = await settings.GetAsync(
            NotificationSettingKeys.DispatchBatchSize,
            NotificationDefaults.DispatchBatchSize,
            cancellationToken);
        var maxAttempts = await settings.GetAsync(
            NotificationSettingKeys.MaxAttempts,
            NotificationDefaults.MaxAttempts,
            cancellationToken);
        var maxBackoff = TimeSpan.FromMinutes(await settings.GetAsync(
            NotificationSettingKeys.MaxBackoffMinutes,
            NotificationDefaults.MaxBackoffMinutes,
            cancellationToken));
        var disableAfter = await settings.GetAsync(
            NotificationSettingKeys.DisableSubscriptionAfterFailures,
            NotificationDefaults.DisableSubscriptionAfterFailures,
            cancellationToken);

        var due = await context.Deliveries
            .Where(delivery =>
                (delivery.Status == DeliveryStatus.Pending || delivery.Status == DeliveryStatus.Retrying)
                && delivery.NextAttemptAtUtc != null
                && delivery.NextAttemptAtUtc <= now)
            .OrderBy(delivery => delivery.NextAttemptAtUtc)
            .Take(Math.Max(batchSize, 1))
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
        {
            return new DispatchSummaryDto(0, 0, 0, 0);
        }

        var notificationIds = due.Select(delivery => delivery.NotificationId).Distinct().ToList();
        var subscriptionIds = due.Select(delivery => delivery.SubscriptionId).Distinct().ToList();

        var notifications = await context.Notifications
            .Where(notification => notificationIds.Contains(notification.Id))
            .ToDictionaryAsync(notification => notification.Id, cancellationToken);
        var subscriptions = await context.Subscriptions
            .Where(subscription => subscriptionIds.Contains(subscription.Id))
            .ToDictionaryAsync(subscription => subscription.Id, cancellationToken);

        var delivered = 0;
        var retrying = 0;
        var deadLettered = 0;

        foreach (var delivery in due)
        {
            if (!notifications.TryGetValue(delivery.NotificationId, out var notification)
                || !subscriptions.TryGetValue(delivery.SubscriptionId, out var subscription))
            {
                delivery.RecordFailure("The notification or subscription no longer exists.", null, maxAttempts, maxBackoff, now);
                deadLettered++;
                continue;
            }

            var sender = senders.FirstOrDefault(candidate => candidate.Channel == delivery.Channel);
            if (sender is null)
            {
                delivery.RecordFailure($"No sender is registered for the '{delivery.Channel}' channel.", null, maxAttempts, maxBackoff, now);
                deadLettered++;
                continue;
            }

            ChannelResult result;
            try
            {
                result = await sender.SendAsync(subscription, notification, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                result = ChannelResult.Failure(exception.Message);
            }

            if (result.Succeeded)
            {
                delivery.RecordSuccess(result.StatusCode, result.Signature, now);
                subscription.RecordDelivered(now);
                delivered++;
                continue;
            }

            delivery.RecordFailure(result.Error ?? "Delivery failed.", result.StatusCode, maxAttempts, maxBackoff, now);
            if (subscription.RecordFailure(disableAfter))
            {
                logger.LogWarning(
                    "Subscription {SubscriptionCode} disabled after {Failures} consecutive delivery failures.",
                    subscription.Code,
                    subscription.ConsecutiveFailures);
            }

            if (delivery.Status == DeliveryStatus.DeadLettered)
            {
                deadLettered++;
            }
            else
            {
                retrying++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return new DispatchSummaryDto(due.Count, delivered, retrying, deadLettered);
    }
}
