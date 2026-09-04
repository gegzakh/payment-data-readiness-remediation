using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace PDR.Rules.IntegrationTests;

public sealed class RulesSecurityTests(SecuredRulesApiFactory factory) : IClassFixture<SecuredRulesApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Reference_data_is_not_public()
    {
        var schemes = await _client.GetAsync("/api/v1/schemes", Token);
        var countries = await _client.GetAsync("/api/v1/countries", Token);
        var effective = await _client.GetAsync("/api/v1/rulesets/effective?schemeCode=SEPA", Token);

        schemes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        countries.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        effective.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authoring_rules_without_a_token_is_unauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/rulesets",
            new { schemeCode = "SEPA", name = "Nope", description = (string?)null },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Activation_rejects_a_forged_token()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/rulesets/{Guid.NewGuid()}/versions/1/activate")
        {
            Content = JsonContent.Create(new { effectiveFrom = "2026-11-15" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await _client.SendAsync(request, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Settings_surface_rejects_anonymous_callers()
    {
        var read = await _client.GetAsync("/api/v1/settings", Token);
        var write = await _client.PutAsJsonAsync("/api/v1/settings/rules.default_scheme_code", new { value = "CBPR" }, Token);

        read.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        write.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_probes_stay_anonymous()
    {
        var response = await _client.GetAsync("/health/live", Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
