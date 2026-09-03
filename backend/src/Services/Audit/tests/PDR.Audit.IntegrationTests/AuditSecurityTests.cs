using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace PDR.Audit.IntegrationTests;

public sealed class AuditSecurityTests(SecuredAuditApiFactory factory) : IClassFixture<SecuredAuditApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_ledger_is_never_anonymous()
    {
        var search = await _client.GetAsync("/api/v1/audit", Token);
        var verify = await _client.GetAsync("/api/v1/audit/verify", Token);

        search.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        verify.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Appending_without_a_token_is_unauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/audit",
            new { service = "rules", action = "forged", entityType = "Ruleset", entityId = "1", actor = "mallory" },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_forged_token_is_rejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/audit");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await _client.SendAsync(request, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task There_is_no_way_to_edit_or_delete_history()
    {
        var id = Guid.NewGuid();

        var update = await _client.PutAsJsonAsync($"/api/v1/audit/{id}", new { actor = "mallory" }, Token);
        var delete = await _client.DeleteAsync($"/api/v1/audit/{id}", Token);

        update.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        delete.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}
