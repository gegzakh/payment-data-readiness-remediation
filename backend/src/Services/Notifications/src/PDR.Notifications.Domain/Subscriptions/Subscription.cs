using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;

namespace PDR.Notifications.Domain.Subscriptions;

/// <summary>
/// Who gets told about what. A subscription binds an event pattern and an optional scope to one delivery
/// channel; webhook subscriptions additionally carry the secret their payloads are signed with, which is
/// never returned by the API (FR-RPT-004, FR-API-002).
/// </summary>
public sealed class Subscription : AggregateRoot
{
    private Subscription()
    {
    }

    private Subscription(
        string code,
        string name,
        string eventPattern,
        DeliveryChannel channel,
        string target,
        string? schemeCodes,
        string? sourceCodes,
        NotificationSeverity minimumSeverity,
        string? signingSecret,
        string owner)
    {
        Code = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(code).ToUpperInvariant(), 64);
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 140);
        EventPattern = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(eventPattern).ToLowerInvariant(), 256);
        Channel = channel;
        Target = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(target), 512);
        SchemeCodes = Normalize(schemeCodes);
        SourceCodes = Normalize(sourceCodes);
        MinimumSeverity = minimumSeverity;
        SigningSecret = signingSecret;
        Owner = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(owner), 140);
        IsEnabled = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Comma-separated event names; <c>*</c> matches a trailing segment, e.g. <c>readiness.*</c>.</summary>
    public string EventPattern { get; private set; } = string.Empty;

    public DeliveryChannel Channel { get; private set; }

    public string Target { get; private set; } = string.Empty;

    public string? SchemeCodes { get; private set; }

    public string? SourceCodes { get; private set; }

    public NotificationSeverity MinimumSeverity { get; private set; }

    public string? SigningSecret { get; private set; }

    public string Owner { get; private set; } = string.Empty;

    public bool IsEnabled { get; private set; }

    public DateTimeOffset? LastDeliveredAtUtc { get; private set; }

    public int ConsecutiveFailures { get; private set; }

    public static Result<Subscription> Create(
        string code,
        string name,
        string eventPattern,
        DeliveryChannel channel,
        string target,
        string? schemeCodes,
        string? sourceCodes,
        NotificationSeverity minimumSeverity,
        string? signingSecret,
        string owner)
    {
        if (RequiresSecret(channel))
        {
            if (string.IsNullOrWhiteSpace(signingSecret))
            {
                return Result.Failure<Subscription>(SubscriptionErrors.SecretRequired);
            }

            if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
            {
                return Result.Failure<Subscription>(SubscriptionErrors.InvalidTarget(target));
            }

            if (uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback)
            {
                return Result.Failure<Subscription>(SubscriptionErrors.InsecureTarget);
            }
        }

        return new Subscription(
            code,
            name,
            eventPattern,
            channel,
            target,
            schemeCodes,
            sourceCodes,
            minimumSeverity,
            signingSecret,
            owner);
    }

    public static bool RequiresSecret(DeliveryChannel channel) =>
        channel is DeliveryChannel.Webhook or DeliveryChannel.ItsmTask;

    public Subscription Update(
        string name,
        string eventPattern,
        string? schemeCodes,
        string? sourceCodes,
        NotificationSeverity minimumSeverity)
    {
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 140);
        EventPattern = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(eventPattern).ToLowerInvariant(), 256);
        SchemeCodes = Normalize(schemeCodes);
        SourceCodes = Normalize(sourceCodes);
        MinimumSeverity = minimumSeverity;
        return this;
    }

    public Subscription RotateSecret(string secret)
    {
        SigningSecret = Ensure.NotNullOrWhiteSpace(secret);
        return this;
    }

    public Subscription SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        if (enabled)
        {
            ConsecutiveFailures = 0;
        }

        return this;
    }

    /// <summary>
    /// Decides whether an event belongs to this subscription: the event name has to match the pattern, the
    /// severity has to clear the floor, and the scope filters have to admit the event's scheme and source.
    /// </summary>
    public bool Matches(string eventType, NotificationSeverity severity, string? schemeCode, string? sourceCode)
    {
        if (!IsEnabled || severity < MinimumSeverity)
        {
            return false;
        }

        var matchesEvent = EventPattern
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(pattern => MatchesPattern(pattern, eventType.ToLowerInvariant()));

        return matchesEvent
               && Admits(SchemeCodes, schemeCode)
               && Admits(SourceCodes, sourceCode);
    }

    public void RecordDelivered(DateTimeOffset atUtc)
    {
        LastDeliveredAtUtc = atUtc;
        ConsecutiveFailures = 0;
    }

    /// <summary>
    /// A target that keeps failing is disabled rather than retried forever, so one dead endpoint cannot
    /// hold up the queue for everyone else.
    /// </summary>
    public bool RecordFailure(int disableAfter)
    {
        ConsecutiveFailures++;
        if (ConsecutiveFailures < disableAfter)
        {
            return false;
        }

        IsEnabled = false;
        return true;
    }

    private static bool MatchesPattern(string pattern, string eventType)
    {
        if (pattern == "*")
        {
            return true;
        }

        if (!pattern.EndsWith(".*", StringComparison.Ordinal))
        {
            return string.Equals(pattern, eventType, StringComparison.Ordinal);
        }

        var prefix = pattern[..^1];
        return eventType.StartsWith(prefix, StringComparison.Ordinal);
    }

    private static bool Admits(string? filter, string? value) =>
        filter is null
        || (value is not null && filter.Split(',').Contains(value.ToUpperInvariant(), StringComparer.Ordinal));

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var items = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

        return Ensure.MaxLength(string.Join(',', items), 512);
    }
}

public static class SubscriptionErrors
{
    public static readonly Error SecretRequired = Error.Validation(
        "SUBSCRIPTION.SECRET_REQUIRED",
        "Webhook and ITSM subscriptions must be created with a signing secret.");

    public static readonly Error InsecureTarget = Error.Validation(
        "SUBSCRIPTION.INSECURE_TARGET",
        "Webhook targets must use HTTPS outside local development.");

    public static Error InvalidTarget(string target) =>
        Error.Validation("SUBSCRIPTION.INVALID_TARGET", $"'{target}' is not an absolute URL.");

    public static Error Duplicate(string code) =>
        Error.Conflict("SUBSCRIPTION.DUPLICATE", $"A subscription with code '{code}' already exists.");

    public static Error NotFound(string code) =>
        Error.NotFound("SUBSCRIPTION.NOT_FOUND", $"Subscription '{code}' was not found.");
}
