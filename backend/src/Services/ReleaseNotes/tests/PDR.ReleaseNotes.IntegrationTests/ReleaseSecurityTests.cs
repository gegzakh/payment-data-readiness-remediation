using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace PDR.ReleaseNotes.IntegrationTests;

public sealed class ReleaseSecurityTests(SecuredReleaseNotesApiFactory factory)
    : IClassFixture<SecuredReleaseNotesApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Public_feed_stays_anonymous()
    {
        var response = await _client.GetAsync("/api/v1/releases", Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Settings_surface_rejects_anonymous_callers()
    {
        var read = await _client.GetAsync("/api/v1/settings", Token);
        var write = await _client.PutAsJsonAsync("/api/v1/settings/Some:Key", new { value = "1" }, Token);

        read.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        write.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Creating_a_release_without_a_token_is_unauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/admin/releases",
            new { version = "6.0.0", title = "Nope", releaseDate = "2031-04-04", summary = (string?)null, entries = Array.Empty<object>() },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_forged_token_is_rejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/releases")
        {
            Content = JsonContent.Create(new
            {
                version = "6.0.1",
                title = "Nope",
                releaseDate = "2031-04-04",
                summary = (string?)null,
                entries = Array.Empty<object>()
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await _client.SendAsync(request, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
