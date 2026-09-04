using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.BuildingBlocks.Persistence.Settings;
using PDR.Notifications.Application.Notifications;

namespace PDR.Notifications.Infrastructure.Persistence;

/// <summary>Seeds the delivery tunables; subscriptions and schedules are always operator-created.</summary>
public sealed class NotificationsSeeder(NotificationsDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new (string Key, string Value, string Type, string Description)[]
        {
            (NotificationSettingKeys.PageSize,
                NotificationDefaults.PageSize.ToString(CultureInfo.InvariantCulture),
                "int",
                "Default page size for notification and delivery listings."),
            (NotificationSettingKeys.MaxAttempts,
                NotificationDefaults.MaxAttempts.ToString(CultureInfo.InvariantCulture),
                "int",
                "Delivery attempts before a delivery is dead-lettered."),
            (NotificationSettingKeys.MaxBackoffMinutes,
                NotificationDefaults.MaxBackoffMinutes.ToString(CultureInfo.InvariantCulture),
                "int",
                "Upper bound on the exponential retry backoff between delivery attempts."),
            (NotificationSettingKeys.MaxPayloadBytes,
                NotificationDefaults.MaxPayloadBytes.ToString(CultureInfo.InvariantCulture),
                "int",
                "Largest notification payload accepted by the publish endpoint."),
            (NotificationSettingKeys.DisableSubscriptionAfterFailures,
                NotificationDefaults.DisableSubscriptionAfterFailures.ToString(CultureInfo.InvariantCulture),
                "int",
                "Consecutive failures after which a subscription is disabled automatically."),
            (NotificationSettingKeys.DispatchBatchSize,
                NotificationDefaults.DispatchBatchSize.ToString(CultureInfo.InvariantCulture),
                "int",
                "Deliveries drained per dispatch pass.")
        };

        var added = false;
        foreach (var (key, value, type, description) in defaults)
        {
            if (!await context.SystemSettings.AnyAsync(setting => setting.Key == key, cancellationToken))
            {
                context.SystemSettings.Add(new SystemSetting(key, value, type, description));
                added = true;
            }
        }

        if (added)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
