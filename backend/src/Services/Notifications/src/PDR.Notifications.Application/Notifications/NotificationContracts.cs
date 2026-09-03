using PDR.Notifications.Domain.Schedules;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.Application.Notifications;

public sealed record SubscriptionDto(
    Guid Id,
    string Code,
    string Name,
    string EventPattern,
    DeliveryChannel Channel,
    string Target,
    string? SchemeCodes,
    string? SourceCodes,
    NotificationSeverity MinimumSeverity,
    string Owner,
    bool IsEnabled,
    bool HasSigningSecret,
    int ConsecutiveFailures,
    DateTimeOffset? LastDeliveredAtUtc);

public sealed record DeliveryDto(
    Guid Id,
    Guid NotificationId,
    string SubscriptionCode,
    DeliveryChannel Channel,
    string Target,
    DeliveryStatus Status,
    int AttemptCount,
    DateTimeOffset QueuedAtUtc,
    DateTimeOffset? NextAttemptAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    int? ResponseStatusCode,
    string? LastError);

public sealed record NotificationDto(
    Guid Id,
    string IdempotencyKey,
    string EventType,
    NotificationSeverity Severity,
    string Subject,
    string Payload,
    string? SchemeCode,
    string? SourceCode,
    string PublishedBy,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyList<DeliveryDto> Deliveries);

public sealed record ScheduledReportDto(
    Guid Id,
    string Code,
    string Name,
    string Audience,
    string? SchemeCodes,
    string? SourceCodes,
    ScheduleFrequency Frequency,
    int HourUtc,
    int DayOfWeek,
    int DayOfMonth,
    string Recipients,
    string Owner,
    bool IsEnabled,
    int RunCount,
    DateTimeOffset? LastRunAtUtc,
    DateTimeOffset? NextRunAtUtc);

/// <summary>What a dispatch pass did, so operators can see progress without reading logs.</summary>
public sealed record DispatchSummaryDto(int Attempted, int Delivered, int Retrying, int DeadLettered);

public static class NotificationSettingKeys
{
    public const string PageSize = "notifications.page-size";
    public const string MaxAttempts = "notifications.max-delivery-attempts";
    public const string MaxBackoffMinutes = "notifications.max-backoff-minutes";
    public const string MaxPayloadBytes = "notifications.max-payload-bytes";
    public const string DisableSubscriptionAfterFailures = "notifications.disable-subscription-after-failures";
    public const string DispatchBatchSize = "notifications.dispatch-batch-size";
}

public static class NotificationDefaults
{
    public const int PageSize = 20;
    public const int MaxPageSize = 200;
    public const int MaxAttempts = 5;
    public const int MaxBackoffMinutes = 60;
    public const int MaxPayloadBytes = 65536;
    public const int DisableSubscriptionAfterFailures = 20;
    public const int DispatchBatchSize = 50;
}

public static class NotificationMapper
{
    public static SubscriptionDto ToDto(this Subscription subscription) =>
        new(
            subscription.Id,
            subscription.Code,
            subscription.Name,
            subscription.EventPattern,
            subscription.Channel,
            subscription.Target,
            subscription.SchemeCodes,
            subscription.SourceCodes,
            subscription.MinimumSeverity,
            subscription.Owner,
            subscription.IsEnabled,
            subscription.SigningSecret is not null,
            subscription.ConsecutiveFailures,
            subscription.LastDeliveredAtUtc);

    public static DeliveryDto ToDto(this Delivery delivery) =>
        new(
            delivery.Id,
            delivery.NotificationId,
            delivery.SubscriptionCode,
            delivery.Channel,
            delivery.Target,
            delivery.Status,
            delivery.AttemptCount,
            delivery.QueuedAtUtc,
            delivery.NextAttemptAtUtc,
            delivery.DeliveredAtUtc,
            delivery.ResponseStatusCode,
            delivery.LastError);

    public static NotificationDto ToDto(this Notification notification) =>
        new(
            notification.Id,
            notification.IdempotencyKey,
            notification.EventType,
            notification.Severity,
            notification.Subject,
            notification.Payload,
            notification.SchemeCode,
            notification.SourceCode,
            notification.PublishedBy,
            notification.OccurredAtUtc,
            [.. notification.Deliveries.OrderBy(delivery => delivery.SubscriptionCode, StringComparer.Ordinal).Select(ToDto)]);

    public static ScheduledReportDto ToDto(this ScheduledReport report) =>
        new(
            report.Id,
            report.Code,
            report.Name,
            report.Audience,
            report.SchemeCodes,
            report.SourceCodes,
            report.Frequency,
            report.HourUtc,
            report.DayOfWeek,
            report.DayOfMonth,
            report.Recipients,
            report.Owner,
            report.IsEnabled,
            report.RunCount,
            report.LastRunAtUtc,
            report.NextRunAtUtc);
}
