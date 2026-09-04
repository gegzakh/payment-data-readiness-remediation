using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.BuildingBlocks.Core.Paging;
using PDR.ReleaseNotes.Application.Releases;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.IntegrationTests;

public sealed class ReleaseFeedTests(ReleaseNotesApiFactory factory) : IClassFixture<ReleaseNotesApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly string[] References = ["FRD-4.2"];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Seeded_release_is_published_and_served()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<ReleaseDto>>("/api/v1/releases", Json, Token);

        page.Should().NotBeNull();
        page.Items.Should().ContainSingle(release => release.Version == "0.1.0")
            .Which.Status.Should().Be(ReleaseStatus.Published);
    }

    [Fact]
    public async Task Authoring_a_release_publishes_it_newest_first_with_grouped_entries()
    {
        var version = $"9.{Random.Shared.Next(1000, 9999)}.0";
        var create = await _client.PostAsJsonAsync(
            "/api/v1/admin/releases",
            new
            {
                version,
                title = "Structured address validation",
                releaseDate = "2030-01-31",
                summary = "ISO 20022 structured address rules.",
                entries = new[]
                {
                    new { type = "Feature", component = "Validation", title = "Structured address rules", body = (string?)null, sortOrder = (int?)0, references = References },
                    new { type = "Fix", component = "Validation", title = "Postcode edge case", body = (string?)null, sortOrder = (int?)1, references = Array.Empty<string>() }
                }
            },
            Token);

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdId = await create.Content.ReadFromJsonAsync<Guid>(Token);

        var publish = await _client.PostAsync($"/api/v1/admin/releases/{createdId}/publish", null, Token);
        publish.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var page = await _client.GetFromJsonAsync<PagedResult<ReleaseDto>>("/api/v1/releases", Json, Token);
        page.Should().NotBeNull();
        page.Items[0].Version.Should().Be(version, "the newest release date must come first");
        page.Items[0].Groups.Should().ContainSingle()
            .Which.Entries.Select(entry => entry.Title)
            .Should().Equal("Structured address rules", "Postcode edge case");
    }

    [Fact]
    public async Task Draft_releases_are_hidden_from_the_public_feed()
    {
        var version = $"8.{Random.Shared.Next(1000, 9999)}.0";
        var create = await _client.PostAsJsonAsync(
            "/api/v1/admin/releases",
            new { version, title = "Unfinished", releaseDate = "2031-02-02", summary = (string?)null, entries = Array.Empty<object>() },
            Token);
        var draftId = await create.Content.ReadFromJsonAsync<Guid>(Token);

        var page = await _client.GetFromJsonAsync<PagedResult<ReleaseDto>>("/api/v1/releases?pageSize=50", Json, Token);
        page!.Items.Should().NotContain(release => release.Version == version);

        var byId = await _client.GetAsync($"/api/v1/releases/{draftId}", Token);
        byId.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Publishing_a_release_without_entries_is_rejected_as_problem_details()
    {
        var create = await _client.PostAsJsonAsync(
            "/api/v1/admin/releases",
            new
            {
                version = $"7.{Random.Shared.Next(1000, 9999)}.0",
                title = "Empty",
                releaseDate = "2031-03-03",
                summary = (string?)null,
                entries = Array.Empty<object>()
            },
            Token);
        var draftId = await create.Content.ReadFromJsonAsync<Guid>(Token);

        var publish = await _client.PostAsync($"/api/v1/admin/releases/{draftId}/publish", null, Token);

        publish.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await publish.Content.ReadAsStringAsync(Token);
        problem.Should().Contain("RELEASE.NO_ENTRIES");
    }

    [Fact]
    public async Task Page_size_comes_from_runtime_settings_and_can_be_changed_over_http()
    {
        var allowed = await _client.GetFromJsonAsync<int[]>("/api/v1/releases/page-sizes", Token);
        allowed.Should().Equal(10, 20, 50);

        var update = await _client.PutAsJsonAsync(
            "/api/v1/settings/ReleaseNotes:Paging:AllowedPageSizes",
            new { value = "5,10" },
            Token);
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updated = await _client.GetFromJsonAsync<int[]>("/api/v1/releases/page-sizes", Token);
        updated.Should().Equal(5, 10);

        var page = await _client.GetFromJsonAsync<PagedResult<ReleaseDto>>("/api/v1/releases?pageSize=5", Json, Token);
        page!.PageSize.Should().Be(5);
    }
}
