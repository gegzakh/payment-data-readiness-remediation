using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace PDR.Sources.IntegrationTests;

public sealed class SourcesSecurityTests(SecuredSourcesApiFactory factory) : IClassFixture<SecuredSourcesApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_source_inventory_is_not_public()
    {
        var list = await _client.GetAsync("/api/v1/sources", Token);
        var readiness = await _client.GetAsync("/api/v1/sources/readiness", Token);

        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        readiness.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Registering_a_source_without_a_token_is_unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/sources", new { code = "NOPE" }, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Attestation_rejects_a_forged_token()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/sources/HUB-EU/attestation")
        {
            Content = JsonContent.Create(new { })
        };
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
