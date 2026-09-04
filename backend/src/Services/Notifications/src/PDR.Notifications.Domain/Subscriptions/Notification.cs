using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;

namespace PDR.Notifications.Domain.Subscriptions;

/// <summary>
/// One thing that happened, plus a delivery per interested subscription. Notifications are keyed by the
/// publisher's idempotency key so a retried publish never fans out twice (FR-API-002).
/// </summary>
public sealed class Notification : AggregateRoot
{
    private readonly List<Delivery> _deliveries = [];

    private Notification()
    {
    }

    private Notification(
        string idempotencyKey,
        string eventType,
        NotificationSeverity severity,
        string subject,
        string payload,
        string? schemeCode,
        string? sourceCode,
        string publishedBy,
        DateTimeOffset occurredAtUtc)
    {
        IdempotencyKey = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(idempotencyKey), 128);
        EventType = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(eventType).ToLowerInvariant(), 128);
        Severity = severity;
        Subject = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(subject), 256);
        Payload = payload;
        SchemeCode = schemeCode?.ToUpperInvariant();
        SourceCode = sourceCode?.ToUpperInvariant();
        PublishedBy = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(publishedBy), 140);
        OccurredAtUtc = occurredAtUtc;
    }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    public NotificationSeverity Severity { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public string? SchemeCode { get; private set; }

    public string? SourceCode { get; private set; }

    public string PublishedBy { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public IReadOnlyCollection<Delivery> Deliveries => _deliveries.AsReadOnly();

    public static Notification Publish(
        string idempotencyKey,
        string eventType,
        NotificationSeverity severity,
        string subject,
        string payload,
        string? schemeCode,
        string? sourceCode,
        string publishedBy,
        DateTimeOffset occurredAtUtc) =>
        new(idempotencyKey, eventType, severity, subject, payload, schemeCode, sourceCode, publishedBy, occurredAtUtc);

    public Delivery AddDelivery(Subscription subscription, DateTimeOffset queuedAtUtc)
    {
        var delivery = new Delivery(Id, subscription.Id, subscription.Code, subscription.Channel, subscription.Target, queuedAtUtc);
        _deliveries.Add(delivery);
        return delivery;
    }
}

/// <summary>
/// One attempt stream at getting a notification to a single subscriber. Failures back off exponentially and
/// dead-letter once the budget is spent, so a broken endpoint degrades on its own rather than looping.
/// </summary>
public sealed class Delivery : Entity
{
    private Delivery()
    {
    }

    internal Delivery(
        Guid notificationId,
        Guid subscriptionId,
        string subscriptionCode,
        DeliveryChannel channel,
        string target,
        DateTimeOffset queuedAtUtc)
    {
        NotificationId = notificationId;
        SubscriptionId = subscriptionId;
        SubscriptionCode = Ensure.MaxLength(subscriptionCode, 64);
        Channel = channel;
        Target = Ensure.MaxLength(target, 512);
        Status = DeliveryStatus.Pending;
        QueuedAtUtc = queuedAtUtc;
        NextAttemptAtUtc = queuedAtUtc;
    }

    public Guid NotificationId { get; private set; }

    public Guid SubscriptionId { get; private set; }

    public string SubscriptionCode { get; private set; } = string.Empty;

    public DeliveryChannel Channel { get; private set; }

    public string Target { get; private set; } = string.Empty;

    public DeliveryStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset QueuedAtUtc { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    public int? ResponseStatusCode { get; private set; }

    public string? LastError { get; private set; }

    public string? Signature { get; private set; }

    public bool IsDue(DateTimeOffset now) =>
        Status is DeliveryStatus.Pending or DeliveryStatus.Retrying
        && NextAttemptAtUtc is not null
        && NextAttemptAtUtc <= now;

    public void RecordSuccess(int? statusCode, string? signature, DateTimeOffset atUtc)
    {
        AttemptCount++;
        Status = DeliveryStatus.Delivered;
        ResponseStatusCode = statusCode;
        Signature = signature is null ? null : Ensure.MaxLength(signature, 256);
        DeliveredAtUtc = atUtc;
        NextAttemptAtUtc = null;
        LastError = null;
    }

    /// <summary>Backs off 1, 2, 4, 8 … minutes up to the cap, then dead-letters when the budget is spent.</summary>
    public void RecordFailure(string error, int? statusCode, int maxAttempts, TimeSpan maxBackoff, DateTimeOffset atUtc)
    {
        AttemptCount++;
        ResponseStatusCode = statusCode;
        LastError = Ensure.MaxLength(error, 512);

        if (AttemptCount >= maxAttempts)
        {
            Status = DeliveryStatus.DeadLettered;
            NextAttemptAtUtc = null;
            return;
        }

        Status = DeliveryStatus.Retrying;
        var backoff = TimeSpan.FromMinutes(Math.Pow(2, AttemptCount - 1));
        NextAttemptAtUtc = atUtc + (backoff > maxBackoff ? maxBackoff : backoff);
    }

    /// <summary>Puts a dead-lettered delivery back in the queue after the operator has fixed the endpoint.</summary>
    public void Replay(DateTimeOffset atUtc)
    {
        Status = DeliveryStatus.Pending;
        AttemptCount = 0;
        NextAttemptAtUtc = atUtc;
        LastError = null;
    }
}

/// <summary>
/// Signs outbound payloads so a receiver can prove the call came from the platform and is not a replay:
/// <c>HMAC-SHA256(secret, "{unix timestamp}.{body}")</c>, matching the header the developer guide documents.
/// </summary>
public static class WebhookSignature
{
    public const string SignatureHeader = "PDR-Signature";
    public const string TimestampHeader = "PDR-Timestamp";

    public static string Compute(string secret, string body, DateTimeOffset timestamp)
    {
        var payload = $"{timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}.{body}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        return $"v1={Convert.ToHexStringLower(hash)}";
    }

    public static bool Verify(string secret, string body, DateTimeOffset timestamp, string signature) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Compute(secret, body, timestamp)),
            Encoding.UTF8.GetBytes(signature));
}

public static class NotificationErrors
{
    public static Error PayloadTooLarge(int bytes, int limit) =>
        Error.Validation(
            "NOTIFICATION.PAYLOAD_TOO_LARGE",
            $"The payload is {bytes} bytes; the configured limit is {limit} bytes.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("NOTIFICATION.NOT_FOUND", $"Notification '{id}' was not found.");

    public static Error DeliveryNotFound(Guid id) =>
        Error.NotFound("NOTIFICATION.DELIVERY_NOT_FOUND", $"Delivery '{id}' was not found.");

    public static readonly Error DeliveryNotReplayable = Error.Conflict(
        "NOTIFICATION.DELIVERY_NOT_REPLAYABLE",
        "Only failed or dead-lettered deliveries can be replayed.");
}
