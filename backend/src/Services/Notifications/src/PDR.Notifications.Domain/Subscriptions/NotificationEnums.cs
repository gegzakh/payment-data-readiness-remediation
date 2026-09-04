namespace PDR.Notifications.Domain.Subscriptions;

public enum DeliveryChannel
{
    /// <summary>Recorded for the recipient to read in the UI; nothing leaves the platform.</summary>
    InApp = 0,
    Email = 1,
    Webhook = 2,

    /// <summary>A webhook whose payload is shaped as a task for a collaboration/ITSM tool (FR-RPT-004).</summary>
    ItsmTask = 3
}

public enum DeliveryStatus
{
    Pending = 0,
    Delivered = 1,

    /// <summary>Failed but still inside its retry budget.</summary>
    Retrying = 2,

    /// <summary>Retries exhausted; kept for inspection and manual replay.</summary>
    DeadLettered = 3
}

public enum ScheduleFrequency
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2
}

public enum NotificationSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}
