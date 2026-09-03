using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.Sources.Application.Inventory;

namespace PDR.Sources.IntegrationTests;

public sealed class SourceInventoryTests(SourcesApiFactory factory) : IClassFixture<SourcesApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static object RegisterPayload(string code) => new
    {
        code,
        name = $"Source {code}",
        kind = "Erp",
        @interface = "Sftp",
        ownerName = "Finance Systems",
        ownerEmail = "finance@example.com",
        legalEntity = "EU-BANK-01",
        schemeCodes = new[] { "SEPA" },
        schedule = "Daily",
        estimatedPartyCount = 5000,
        recurringInstructionCount = 250,
        isAuthoritative = true
    };

    private async Task<SourceSystemDto> GetAsync(string code)
    {
        var response = await _client.GetAsync($"/api/v1/sources/{code}", Token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<SourceSystemDto>(Json, Token))!;
    }

    [Fact]
    public async Task Seeded_inventory_is_available_with_a_portfolio_summary()
    {
        var response = await _client.GetAsync("/api/v1/sources", Token);
        var sources = await response.Content.ReadFromJsonAsync<List<SourceSystemDto>>(Json, Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        sources.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(3);

        var readiness = await _client.GetFromJsonAsync<SourceReadinessSummaryDto>("/api/v1/sources/readiness", Json, Token);

        readiness!.TotalSources.Should().BeGreaterThanOrEqualTo(3);
        readiness.TotalPartyCount.Should().BeGreaterThan(readiness.CoveredPartyCount);
        readiness.AverageReadinessScore.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task A_source_can_be_registered_mapped_scanned_and_attested()
    {
        var code = $"SRC{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        var created = await _client.PostAsJsonAsync("/api/v1/sources", RegisterPayload(code), Token);
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var mapping = await _client.PostAsJsonAsync(
            $"/api/v1/sources/{code}/mappings",
            new
            {
                sourceAttribute = "SUPPLIER.POSTCODE",
                targetElement = "PstlAdr/PstCd",
                transformation = (string?)null,
                isAuthoritative = true,
                notes = (string?)null
            },
            Token);
        mapping.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var lineage = await _client.PutAsJsonAsync(
            $"/api/v1/sources/{code}/lineage",
            new
            {
                steps = new[]
                {
                    new { fromNode = "Supplier master", toNode = "AP file", channel = "SFTP", description = (string?)null },
                    new { fromNode = "AP file", toNode = "Payment hub", channel = "File", description = (string?)null }
                }
            },
            Token);
        lineage.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var scan = await _client.PostAsJsonAsync($"/api/v1/sources/{code}/scan", new { coveragePercent = 75.5m }, Token);
        scan.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var attest = await _client.PostAsJsonAsync($"/api/v1/sources/{code}/attestation", new { }, Token);
        attest.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var source = await GetAsync(code);

        source.Mappings.Should().ContainSingle(entry => entry.TargetElement == "PstlAdr/PstCd");
        source.Lineage.Select(step => step.Sequence).Should().Equal(1, 2);
        source.ScanCoveragePercent.Should().Be(75.5m);
        source.AttestationOverdue.Should().BeFalse();
        source.ReadinessScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Registering_the_same_code_twice_conflicts()
    {
        var code = $"DUP{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        (await _client.PostAsJsonAsync("/api/v1/sources", RegisterPayload(code), Token))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/v1/sources", RegisterPayload(code), Token);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Duplicate_field_mappings_are_rejected()
    {
        var code = $"MAP{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        await _client.PostAsJsonAsync("/api/v1/sources", RegisterPayload(code), Token);

        var payload = new
        {
            sourceAttribute = "SUPPLIER.CITY",
            targetElement = "PstlAdr/TwnNm",
            transformation = (string?)null,
            isAuthoritative = true,
            notes = (string?)null
        };

        await _client.PostAsJsonAsync($"/api/v1/sources/{code}/mappings", payload, Token);
        var duplicate = await _client.PostAsJsonAsync($"/api/v1/sources/{code}/mappings", payload, Token);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Unknown_sources_return_not_found_and_invalid_coverage_is_rejected()
    {
        var missing = await _client.GetAsync("/api/v1/sources/DOES-NOT-EXIST", Token);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var code = $"SCN{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        await _client.PostAsJsonAsync("/api/v1/sources", RegisterPayload(code), Token);

        var scan = await _client.PostAsJsonAsync($"/api/v1/sources/{code}/scan", new { coveragePercent = 250 }, Token);

        scan.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Overdue_attestations_can_be_filtered()
    {
        var response = await _client.GetAsync("/api/v1/sources?attestationOverdueOnly=true", Token);
        var overdue = await response.Content.ReadFromJsonAsync<List<SourceSystemDto>>(Json, Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        overdue.Should().NotBeNull().And.OnlyContain(source => source.AttestationOverdue);
    }
}
