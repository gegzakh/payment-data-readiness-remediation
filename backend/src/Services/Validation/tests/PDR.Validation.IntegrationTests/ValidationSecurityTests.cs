using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace PDR.Validation.IntegrationTests;

public sealed class ValidationSecurityTests(SecuredValidationApiFactory factory)
    : IClassFixture<SecuredValidationApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Readiness_and_assessments_are_never_public()
    {
        var readiness = await _client.GetAsync("/api/v1/validation/readiness", Token);
        var runs = await _client.GetAsync("/api/v1/validation/runs", Token);
        var assessments = await _client.GetAsync($"/api/v1/validation/runs/{Guid.NewGuid()}/assessments", Token);

        readiness.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        runs.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        assessments.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Starting_a_run_without_a_token_is_unauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/validation/runs",
            new { batchId = Guid.NewGuid() },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_forged_token_is_rejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/validation/runs");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await _client.SendAsync(request, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_probes_stay_anonymous()
    {
        var response = await _client.GetAsync("/health/live", Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
