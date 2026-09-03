using AwesomeAssertions;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.UnitTests;

public sealed class DeliveryTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_delivery_is_due_immediately()
    {
        var delivery = Queue();

        delivery.Status.Should().Be(DeliveryStatus.Pending);
        delivery.IsDue(Now).Should().BeTrue();
    }

    [Fact]
    public void Failures_back_off_exponentially_up_to_the_cap()
    {
        var delivery = Queue();
        var cap = TimeSpan.FromMinutes(3);

        delivery.RecordFailure("boom", 500, maxAttempts: 5, cap, Now);
        delivery.NextAttemptAtUtc.Should().Be(Now.AddMinutes(1));

        delivery.RecordFailure("boom", 500, maxAttempts: 5, cap, Now);
        delivery.NextAttemptAtUtc.Should().Be(Now.AddMinutes(2));

        delivery.RecordFailure("boom", 500, maxAttempts: 5, cap, Now);
        delivery.NextAttemptAtUtc.Should().Be(Now.Add(cap));
        delivery.Status.Should().Be(DeliveryStatus.Retrying);
    }

    [Fact]
    public void A_delivery_dead_letters_once_the_attempt_budget_is_spent()
    {
        var delivery = Queue();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            delivery.RecordFailure("boom", 500, maxAttempts: 3, TimeSpan.FromMinutes(60), Now);
        }

        delivery.Status.Should().Be(DeliveryStatus.DeadLettered);
        delivery.NextAttemptAtUtc.Should().BeNull();
        delivery.IsDue(Now.AddDays(1)).Should().BeFalse();
        delivery.LastError.Should().Be("boom");
    }

    [Fact]
    public void Replay_requeues_a_dead_lettered_delivery_with_a_fresh_budget()
    {
        var delivery = Queue();
        delivery.RecordFailure("boom", 500, maxAttempts: 1, TimeSpan.FromMinutes(60), Now);

        delivery.Replay(Now.AddHours(1));

        delivery.Status.Should().Be(DeliveryStatus.Pending);
        delivery.AttemptCount.Should().Be(0);
        delivery.LastError.Should().BeNull();
        delivery.IsDue(Now.AddHours(1)).Should().BeTrue();
    }

    [Fact]
    public void Success_records_the_signature_and_stops_further_attempts()
    {
        var delivery = Queue();

        delivery.RecordSuccess(202, "v1=abc", Now);

        delivery.Status.Should().Be(DeliveryStatus.Delivered);
        delivery.Signature.Should().Be("v1=abc");
        delivery.DeliveredAtUtc.Should().Be(Now);
        delivery.IsDue(Now.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void A_notification_only_queues_deliveries_for_the_subscriptions_it_is_given()
    {
        var notification = Notification.Publish(
            "key-1",
            "Validation.Completed",
            NotificationSeverity.Info,
            "Validation finished",
            "{}",
            "sepa",
            "cbs",
            "tester",
            Now);

        notification.AddDelivery(Subscribe("OPS"), Now);
        notification.AddDelivery(Subscribe("RISK"), Now);

        notification.EventType.Should().Be("validation.completed");
        notification.SchemeCode.Should().Be("SEPA");
        notification.Deliveries.Select(delivery => delivery.SubscriptionCode)
            .Should().BeEquivalentTo(["OPS", "RISK"]);
    }

    [Fact]
    public void A_signature_verifies_only_against_the_same_secret_body_and_timestamp()
    {
        const string body = """{"eventType":"validation.completed"}""";
        var signature = WebhookSignature.Compute("s3cret", body, Now);

        WebhookSignature.Verify("s3cret", body, Now, signature).Should().BeTrue();
        WebhookSignature.Verify("other", body, Now, signature).Should().BeFalse();
        WebhookSignature.Verify("s3cret", body + " ", Now, signature).Should().BeFalse();
        WebhookSignature.Verify("s3cret", body, Now.AddSeconds(1), signature).Should().BeFalse();
        signature.Should().StartWith("v1=");
    }

    private static Delivery Queue()
    {
        var notification = Notification.Publish(
            "key-1",
            "validation.completed",
            NotificationSeverity.Info,
            "Validation finished",
            "{}",
            null,
            null,
            "tester",
            Now);

        return notification.AddDelivery(Subscribe("OPS"), Now);
    }

    private static Subscription Subscribe(string code) =>
        Subscription.Create(
            code,
            code,
            "*",
            DeliveryChannel.InApp,
            "ops-team",
            null,
            null,
            NotificationSeverity.Info,
            null,
            "tester").Value;
}
