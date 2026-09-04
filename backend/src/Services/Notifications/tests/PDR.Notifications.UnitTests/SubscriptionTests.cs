using AwesomeAssertions;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.UnitTests;

public sealed class SubscriptionTests
{
    [Fact]
    public void A_webhook_without_a_signing_secret_is_rejected()
    {
        var result = Create(DeliveryChannel.Webhook, "https://ops.example.com/hook", secret: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SUBSCRIPTION.SECRET_REQUIRED");
    }

    [Fact]
    public void A_webhook_target_that_is_not_https_is_rejected()
    {
        var result = Create(DeliveryChannel.Webhook, "http://ops.example.com/hook", "s3cret");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SUBSCRIPTION.INSECURE_TARGET");
    }

    [Fact]
    public void A_loopback_target_is_allowed_so_the_dev_stack_can_receive_webhooks()
    {
        Create(DeliveryChannel.Webhook, "http://localhost:9999/hook", "s3cret").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void An_in_app_subscription_needs_no_secret()
    {
        Create(DeliveryChannel.InApp, "ops-team", secret: null).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("*", "validation.completed", true)]
    [InlineData("validation.*", "validation.completed", true)]
    [InlineData("validation.*", "remediation.approved", false)]
    [InlineData("validation.completed", "validation.completed", true)]
    [InlineData("validation.completed", "validation.failed", false)]
    [InlineData("validation.completed,cutover.signed-off", "cutover.signed-off", true)]
    public void Event_patterns_decide_what_a_subscription_receives(string pattern, string eventType, bool expected)
    {
        var subscription = Enabled(pattern: pattern);

        subscription.Matches(eventType, NotificationSeverity.Info, null, null).Should().Be(expected);
    }

    [Fact]
    public void Events_below_the_severity_floor_are_skipped()
    {
        var subscription = Enabled(severity: NotificationSeverity.Critical);

        subscription.Matches("validation.completed", NotificationSeverity.Warning, null, null).Should().BeFalse();
        subscription.Matches("validation.completed", NotificationSeverity.Critical, null, null).Should().BeTrue();
    }

    [Fact]
    public void Scope_filters_admit_only_the_subscribed_schemes_and_sources()
    {
        var subscription = Enabled(schemeCodes: "sepa", sourceCodes: "cbs");

        subscription.Matches("validation.completed", NotificationSeverity.Info, "SEPA", "CBS").Should().BeTrue();
        subscription.Matches("validation.completed", NotificationSeverity.Info, "SWIFT", "CBS").Should().BeFalse();
        subscription.Matches("validation.completed", NotificationSeverity.Info, "SEPA", "LEGACY").Should().BeFalse();
        subscription.Matches("validation.completed", NotificationSeverity.Info, null, null).Should().BeFalse();
    }

    [Fact]
    public void A_disabled_subscription_matches_nothing()
    {
        var subscription = Enabled().SetEnabled(false);

        subscription.Matches("validation.completed", NotificationSeverity.Critical, null, null).Should().BeFalse();
    }

    [Fact]
    public void A_target_that_keeps_failing_is_disabled_and_re_enabling_clears_the_counter()
    {
        var subscription = Enabled();

        subscription.RecordFailure(3).Should().BeFalse();
        subscription.RecordFailure(3).Should().BeFalse();
        subscription.RecordFailure(3).Should().BeTrue();
        subscription.IsEnabled.Should().BeFalse();

        subscription.SetEnabled(true);
        subscription.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void A_successful_delivery_clears_the_failure_streak()
    {
        var subscription = Enabled();
        subscription.RecordFailure(10);

        subscription.RecordDelivered(DateTimeOffset.UnixEpoch);

        subscription.ConsecutiveFailures.Should().Be(0);
        subscription.LastDeliveredAtUtc.Should().Be(DateTimeOffset.UnixEpoch);
    }

    private static Subscription Enabled(
        string pattern = "*",
        NotificationSeverity severity = NotificationSeverity.Info,
        string? schemeCodes = null,
        string? sourceCodes = null) =>
        Subscription.Create(
            "OPS",
            "Operations",
            pattern,
            DeliveryChannel.InApp,
            "ops-team",
            schemeCodes,
            sourceCodes,
            severity,
            null,
            "tester").Value.SetEnabled(true);

    private static PDR.BuildingBlocks.Core.Results.Result<Subscription> Create(
        DeliveryChannel channel,
        string target,
        string? secret) =>
        Subscription.Create(
            "OPS",
            "Operations",
            "*",
            channel,
            target,
            null,
            null,
            NotificationSeverity.Info,
            secret,
            "tester");
}
