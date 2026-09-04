using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PDR.BuildingBlocks.Core.Time;
using PDR.Notifications.Application.Abstractions;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.Infrastructure.Channels;

/// <summary>
/// In-app deliveries need no transport: the notification row itself is what the UI reads, so recording the
/// attempt is the delivery.
/// </summary>
public sealed class InAppChannelSender : IChannelSender
{
    public DeliveryChannel Channel => DeliveryChannel.InApp;

    public Task<ChannelResult> SendAsync(
        Subscription subscription,
        Notification notification,
        CancellationToken cancellationToken) =>
        Task.FromResult(ChannelResult.Success());
}

/// <summary>
/// There is no SMTP relay in the dev stack, so email is logged rather than sent. The delivery record still
/// carries the target and attempt history, and swapping in a real relay only replaces this class.
/// </summary>
public sealed class EmailChannelSender(ILogger<EmailChannelSender> logger) : IChannelSender
{
    public DeliveryChannel Channel => DeliveryChannel.Email;

    public Task<ChannelResult> SendAsync(
        Subscription subscription,
        Notification notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Email notification {EventType} for {Subject} routed to {Target}.",
            notification.EventType,
            notification.Subject,
            subscription.Target);

        return Task.FromResult(ChannelResult.Success());
    }
}

/// <summary>
/// Posts the signed event envelope to the subscriber's endpoint. 2xx is success; anything else — including a
/// transport failure — is reported as a failure so the dispatcher can back off and eventually dead-letter.
/// </summary>
public sealed class WebhookChannelSender(HttpClient httpClient, IClock clock) : IChannelSender
{
    public DeliveryChannel Channel => DeliveryChannel.Webhook;

    private static object BuildEnvelope(Notification notification) =>
        new
        {
            id = notification.Id,
            type = notification.EventType,
            severity = notification.Severity.ToString(),
            subject = notification.Subject,
            occurredAtUtc = notification.OccurredAtUtc,
            scheme = notification.SchemeCode,
            source = notification.SourceCode,
            data = notification.Payload
        };

    public async Task<ChannelResult> SendAsync(
        Subscription subscription,
        Notification notification,
        CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(BuildEnvelope(notification));
        var timestamp = clock.UtcNow;
        var signature = WebhookSignature.Compute(subscription.SigningSecret ?? string.Empty, body, timestamp);

        using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Target)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation(WebhookSignature.SignatureHeader, signature);
        request.Headers.TryAddWithoutValidation(
            WebhookSignature.TimestampHeader,
            timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        request.Headers.TryAddWithoutValidation("PDR-Event-Id", notification.Id.ToString());

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? ChannelResult.Success((int)response.StatusCode, signature)
            : ChannelResult.Failure(
                $"The endpoint responded {(int)response.StatusCode} {response.ReasonPhrase}.",
                (int)response.StatusCode);
    }
}

/// <summary>
/// Same transport as a webhook, but the body is shaped as a ticket so a collaboration/ITSM tool can create
/// work from it directly instead of needing a bespoke adapter (FR-RPT-004).
/// </summary>
public sealed class ItsmTaskChannelSender(HttpClient httpClient, IClock clock) : IChannelSender
{
    public DeliveryChannel Channel => DeliveryChannel.ItsmTask;

    public async Task<ChannelResult> SendAsync(
        Subscription subscription,
        Notification notification,
        CancellationToken cancellationToken)
    {
        var task = new
        {
            summary = notification.Subject,
            description = notification.Payload,
            priority = notification.Severity switch
            {
                NotificationSeverity.Critical => "High",
                NotificationSeverity.Warning => "Medium",
                _ => "Low"
            },
            labels = new[] { "payment-data-readiness", notification.EventType },
            externalId = notification.Id,
            raisedAtUtc = notification.OccurredAtUtc
        };

        var body = JsonSerializer.Serialize(task);
        var timestamp = clock.UtcNow;
        var signature = WebhookSignature.Compute(subscription.SigningSecret ?? string.Empty, body, timestamp);

        using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Target)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation(WebhookSignature.SignatureHeader, signature);
        request.Headers.TryAddWithoutValidation(
            WebhookSignature.TimestampHeader,
            timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? ChannelResult.Success((int)response.StatusCode, signature)
            : ChannelResult.Failure(
                $"The ITSM endpoint responded {(int)response.StatusCode} {response.ReasonPhrase}.",
                (int)response.StatusCode);
    }
}
