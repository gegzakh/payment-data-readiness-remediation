using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AwesomeAssertions;

namespace PDR.Ingestion.IntegrationTests;

public sealed class IngestionSecurityTests(SecuredIngestionApiFactory factory) : IClassFixture<SecuredIngestionApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Batch_data_is_never_public()
    {
        var listing = await _client.GetAsync("/api/v1/batches", Token);
        var records = await _client.GetAsync($"/api/v1/batches/{Guid.NewGuid()}/records", Token);

        listing.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        records.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Uploading_without_a_token_is_unauthorized()
    {
        using var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(Encoding.UTF8.GetBytes("PartyRole\nDebtor")), "file", "feed.csv" }
        };

        var response = await _client.PostAsync("/api/v1/batches/upload?sourceCode=HUB-EU&format=Csv", form, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_forged_token_is_rejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/batches");
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
