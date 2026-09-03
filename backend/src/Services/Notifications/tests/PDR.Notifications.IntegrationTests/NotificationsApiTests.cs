using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.BuildingBlocks.Core.Paging;
using PDR.Notifications.Application.Notifications;

namespace PDR.Notifications.IntegrationTests;

public sealed class NotificationsApiTests(NotificationsApiFactory factory) : IClassFixture<NotificationsApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task A_subscription_is_created_and_never_gives_its_secret_back()
    {
        var created = await CreateSubscriptionAsync(
            "WEBHOOK-OPS",
            "*",
            channel: "Webhook",
            target: "https://ops.example.com/hook",
            secret: "s3cret-value");

        created.HasSigningSecret.Should().BeTrue();

        var raw = await _client.GetStringAsync("/api/v1/notifications/subscriptions", Token);
        raw.Should().NotContain("s3cret-value");
    }

    [Fact]
    public async Task A_webhook_subscription_without_a_secret_is_refused()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/notifications/subscriptions",
            new
            {
                code = "WEBHOOK-NOSECRET",
                name = "No secret",
                eventPattern = "*",
                channel = "Webhook",
                target = "https://ops.example.com/hook",
                minimumSeverity = "Info"
            },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Publishing_fans_out_only_to_matching_subscriptions()
    {
        await CreateSubscriptionAsync("FANOUT-SEPA", "validation.*", schemeCodes: "SEPA");
        await CreateSubscriptionAsync("FANOUT-SWIFT", "validation.*", schemeCodes: "SWIFT");
        await CreateSubscriptionAsync("FANOUT-OTHER", "remediation.*");

        var notification = await PublishAsync("fanout-1", "validation.completed", schemeCode: "SEPA");

        notification.Deliveries.Select(delivery => delivery.SubscriptionCode)
            .Should().Contain("FANOUT-SEPA")
            .And.NotContain("FANOUT-SWIFT")
            .And.NotContain("FANOUT-OTHER");
    }

    [Fact]
    public async Task Republishing_the_same_idempotency_key_returns_the_original_notification()
    {
        var first = await PublishAsync("idem-1", "validation.completed");
        var second = await PublishAsync("idem-1", "validation.completed");

        second.Id.Should().Be(first.Id);
        second.Deliveries.Should().HaveCount(first.Deliveries.Count);
    }

    [Fact]
    public async Task A_payload_over_the_configured_limit_is_refused()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/notifications/events",
            new
            {
                idempotencyKey = "too-big",
                eventType = "validation.completed",
                severity = "Info",
                subject = "Big",
                payload = new string('x', NotificationDefaults.MaxPayloadBytes + 1)
            },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task The_idempotency_key_can_travel_in_the_header()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/notifications/events")
        {
            Content = JsonContent.Create(new
            {
                eventType = "validation.completed",
                severity = "Info",
                subject = "Header keyed",
                payload = "{}"
            })
        };
        request.Headers.Add("Idempotency-Key", "header-key-1");

        var response = await _client.SendAsync(request, Token);
        var notification = await Read<NotificationDto>(response);

        notification.IdempotencyKey.Should().Be("header-key-1");
    }

    [Fact]
    public async Task Dispatching_delivers_in_app_notifications_and_records_the_attempt()
    {
        await CreateSubscriptionAsync("DISPATCH-INAPP", "cutover.*");
        await PublishAsync("dispatch-1", "cutover.signed-off");

        var summary = await Post<DispatchSummaryDto>("/api/v1/notifications/deliveries/dispatch");

        summary.Attempted.Should().BeGreaterThan(0);
        summary.Delivered.Should().BeGreaterThan(0);

        var deliveries = await Get<PagedResult<DeliveryDto>>(
            "/api/v1/notifications/deliveries?subscriptionCode=DISPATCH-INAPP");

        deliveries.Items.Should().ContainSingle()
            .Which.Status.Should().Be(Domain.Subscriptions.DeliveryStatus.Delivered);
    }

    [Fact]
    public async Task A_webhook_to_a_dead_endpoint_backs_off_and_can_be_replayed()
    {
        await CreateSubscriptionAsync(
            "DEAD-HOOK",
            "webhook.*",
            channel: "Webhook",
            target: "http://localhost:9/hook",
            secret: "s3cret");
        await PublishAsync("dead-1", "webhook.test");

        await Post<DispatchSummaryDto>("/api/v1/notifications/deliveries/dispatch");

        var deliveries = await Get<PagedResult<DeliveryDto>>(
            "/api/v1/notifications/deliveries?subscriptionCode=DEAD-HOOK");
        var delivery = deliveries.Items.Should().ContainSingle().Subject;

        delivery.Status.Should().Be(Domain.Subscriptions.DeliveryStatus.Retrying);
        delivery.AttemptCount.Should().Be(1);
        delivery.NextAttemptAtUtc.Should().NotBeNull();
        delivery.LastError.Should().NotBeNullOrWhiteSpace();

        var replayed = await Post<DeliveryDto>($"/api/v1/notifications/deliveries/{delivery.Id}/replay");
        replayed.Status.Should().Be(Domain.Subscriptions.DeliveryStatus.Pending);
        replayed.AttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task A_delivered_delivery_cannot_be_replayed()
    {
        await CreateSubscriptionAsync("REPLAY-GUARD", "guard.*");
        await PublishAsync("guard-1", "guard.test");
        await Post<DispatchSummaryDto>("/api/v1/notifications/deliveries/dispatch");

        var deliveries = await Get<PagedResult<DeliveryDto>>(
            "/api/v1/notifications/deliveries?subscriptionCode=REPLAY-GUARD");
        var delivery = deliveries.Items.Should().ContainSingle().Subject;

        var response = await _client.PostAsync(
            $"/api/v1/notifications/deliveries/{delivery.Id}/replay",
            null,
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_scheduled_report_runs_on_demand_and_moves_its_next_slot_forward()
    {
        await CreateSubscriptionAsync("REPORT-SUB", "report.*");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/notifications/scheduled-reports",
            new
            {
                code = "EXEC-DAILY",
                name = "Executive daily",
                audience = "executive",
                frequency = "Daily",
                hourUtc = 6,
                dayOfWeek = 1,
                dayOfMonth = 1,
                recipients = "ops@example.com"
            },
            Token);

        var created = await Read<ScheduledReportDto>(response);
        created.NextRunAtUtc.Should().NotBeNull();

        var ran = await Post<ScheduledReportDto>("/api/v1/notifications/scheduled-reports/EXEC-DAILY/run");

        ran.RunCount.Should().Be(1);
        ran.NextRunAtUtc.Should().BeAfter(created.NextRunAtUtc!.Value.AddMinutes(-1));

        var notifications = await Get<PagedResult<NotificationDto>>(
            "/api/v1/notifications/events?eventType=report.executive");
        notifications.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Notifications_are_paged()
    {
        for (var index = 0; index < 3; index++)
        {
            await PublishAsync($"page-{index}", "paging.test");
        }

        var page = await Get<PagedResult<NotificationDto>>(
            "/api/v1/notifications/events?eventType=paging.test&page=1&pageSize=2");

        page.Items.Should().HaveCount(2);
        page.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    private async Task<SubscriptionDto> CreateSubscriptionAsync(
        string code,
        string eventPattern,
        string channel = "InApp",
        string target = "ops-team",
        string? secret = null,
        string? schemeCodes = null)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/notifications/subscriptions",
            new
            {
                code,
                name = code,
                eventPattern,
                channel,
                target,
                schemeCodes,
                sourceCodes = (string?)null,
                minimumSeverity = "Info",
                signingSecret = secret
            },
            Token);

        return await Read<SubscriptionDto>(response);
    }

    private async Task<NotificationDto> PublishAsync(
        string key,
        string eventType,
        string? schemeCode = null)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/notifications/events",
            new
            {
                idempotencyKey = key,
                eventType,
                severity = "Info",
                subject = eventType,
                payload = """{"detail":"test"}""",
                schemeCode
            },
            Token);

        return await Read<NotificationDto>(response);
    }

    private async Task<T> Get<T>(string url)
    {
        var response = await _client.GetAsync(url, Token);
        return await Read<T>(response);
    }

    private async Task<T> Post<T>(string url)
    {
        var response = await _client.PostAsync(url, null, Token);
        return await Read<T>(response);
    }

    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "the request should succeed but returned {0}: {1}",
            response.StatusCode,
            await response.Content.ReadAsStringAsync(Token));

        var value = await response.Content.ReadFromJsonAsync<T>(Json, Token);
        value.Should().NotBeNull();
        return value!;
    }
}
