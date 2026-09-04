using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace PDR.Notifications.IntegrationTests;

/// <summary>
/// Subscriptions carry signing secrets and can push platform data to third-party endpoints, so nothing
/// here may be read or written without an authenticated caller holding the permission (FR-SEC-001).
/// </summary>
public sealed class NotificationsSecurityTests(SecuredNotificationsApiFactory factory)
    : IClassFixture<SecuredNotificationsApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public static TheoryData<string> ProtectedReads =>
    [
        "/api/v1/notifications/subscriptions",
        "/api/v1/notifications/subscriptions/OPS",
        "/api/v1/notifications/events",
        $"/api/v1/notifications/events/{Guid.Empty}",
        "/api/v1/notifications/deliveries",
        "/api/v1/notifications/scheduled-reports"
    ];

    [Theory]
    [MemberData(nameof(ProtectedReads))]
    public async Task Nothing_is_readable_anonymously(string path)
    {
        var response = await _client.GetAsync(path, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Publishing_subscribing_and_rotating_secrets_reject_anonymous_callers()
    {
        var publish = await _client.PostAsJsonAsync(
            "/api/v1/notifications/events",
            new { eventType = "validation.completed", severity = "Info", subject = "x", payload = "{}" },
            Token);
        var subscribe = await _client.PostAsJsonAsync(
            "/api/v1/notifications/subscriptions",
            new { code = "X", name = "X", eventPattern = "*", channel = "InApp", target = "ops", minimumSeverity = "Info" },
            Token);
        var rotate = await _client.PostAsJsonAsync(
            "/api/v1/notifications/subscriptions/OPS/secret",
            new { secret = "new-secret" },
            Token);
        var dispatch = await _client.PostAsync("/api/v1/notifications/deliveries/dispatch", null, Token);

        publish.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        subscribe.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        rotate.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        dispatch.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_forged_bearer_token_is_rejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/notifications/subscriptions");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer not-a-real-token");

        var response = await _client.SendAsync(request, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_stays_reachable_for_operations()
    {
        var health = await _client.GetAsync("/health/live", Token);

        health.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
