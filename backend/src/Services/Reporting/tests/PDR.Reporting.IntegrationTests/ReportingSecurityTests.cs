using System.Net;
using AwesomeAssertions;

namespace PDR.Reporting.IntegrationTests;

/// <summary>
/// Dashboards aggregate the whole payment portfolio, so no figure and no export may leave the service
/// without an authenticated caller holding the matching permission (FR-SEC-001).
/// </summary>
public sealed class ReportingSecurityTests(SecuredReportingApiFactory factory)
    : IClassFixture<SecuredReportingApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public static TheoryData<string> ProtectedReads =>
    [
        "/api/v1/reporting/dashboards",
        "/api/v1/reporting/dashboards/executive",
        "/api/v1/reporting/dashboards/executive/drill/Scheme",
        "/api/v1/reporting/dashboards/executive/export",
        "/api/v1/reporting/snapshots"
    ];

    [Theory]
    [MemberData(nameof(ProtectedReads))]
    public async Task No_dashboard_data_is_readable_anonymously(string path)
    {
        var response = await _client.GetAsync(path, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_forged_bearer_token_is_rejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reporting/dashboards");
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
