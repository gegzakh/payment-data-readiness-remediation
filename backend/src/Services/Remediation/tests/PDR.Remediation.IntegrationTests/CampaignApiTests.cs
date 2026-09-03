using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.BuildingBlocks.Core.Paging;
using PDR.Remediation.Application.Campaigns;
using PDR.Remediation.Application.Cases;
using PDR.Remediation.Application.Upstream;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.IntegrationTests;

/// <summary>Campaigns group cases for a team or a corporate customer and track their progress (FR-WF-006).</summary>
public sealed class CampaignApiTests(RemediationApiFactory factory) : IClassFixture<RemediationApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly HttpClient _client = factory.CreateClientAs("maker");

    [Fact]
    public async Task A_campaign_is_created_and_returned_with_its_location()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/remediation/campaigns",
            new
            {
                code = "q2-corporates",
                name = "Q2 corporate address clean-up",
                audience = nameof(CampaignAudience.CorporateCustomer),
                assignee = "Acme GmbH",
                dueDate = "2026-06-30",
                description = "Structured addresses for the top 50 corporates"
            },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().EndWith("/api/v1/remediation/campaigns/Q2-CORPORATES");

        var campaign = (await response.Content.ReadFromJsonAsync<CampaignDto>(Json, Token))!;
        campaign.Audience.Should().Be(CampaignAudience.CorporateCustomer);
        campaign.Status.Should().Be(CampaignStatus.Draft);
        campaign.CaseCount.Should().Be(0);
    }

    [Fact]
    public async Task The_same_campaign_code_cannot_be_created_twice()
    {
        var payload = new
        {
            code = "duplicate",
            name = "Duplicate",
            audience = nameof(CampaignAudience.InternalTeam),
            assignee = "Data Team",
            dueDate = "2026-06-30"
        };

        (await _client.PostAsJsonAsync("/api/v1/remediation/campaigns", payload, Token))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        (await _client.PostAsJsonAsync("/api/v1/remediation/campaigns", payload, Token))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Assigning_cases_activates_the_campaign_and_tags_the_cases()
    {
        var caseId = await GeneratedCaseAsync("CAMP", "Campaign GmbH");

        var created = await ReadAsync<CampaignDto>(await _client.PostAsJsonAsync(
            "/api/v1/remediation/campaigns",
            new
            {
                code = "wave-1",
                name = "Wave 1",
                audience = nameof(CampaignAudience.InternalTeam),
                assignee = "Data Team",
                dueDate = "2026-06-30"
            },
            Token));

        var assigned = await ReadAsync<CampaignDto>(await _client.PostAsJsonAsync(
            $"/api/v1/remediation/campaigns/{created.Code}/cases",
            new { caseIds = new[] { caseId } },
            Token));

        assigned.CaseCount.Should().Be(1);
        assigned.Status.Should().Be(CampaignStatus.Active);
        assigned.CompletionPercent.Should().Be(0m);

        var detail = await ReadAsync<CaseDetailDto>(
            await _client.GetAsync($"/api/v1/remediation/cases/{caseId}", Token));

        detail.CampaignId.Should().Be(created.Id);

        var byCampaign = await ReadAsync<PagedResult<CaseListItemDto>>(
            await _client.GetAsync($"/api/v1/remediation/cases?campaignId={created.Id}", Token));

        byCampaign.Items.Should().ContainSingle(item => item.Id == caseId);
    }

    [Fact]
    public async Task Assigning_to_an_unknown_campaign_is_not_found()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/remediation/campaigns/nope/cases",
            new { caseIds = new[] { Guid.NewGuid() } },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> GeneratedCaseAsync(string sourceCode, string partyName)
    {
        var runId = Guid.NewGuid();
        var assessment = new AssessedAddress(
            Guid.NewGuid(),
            sourceCode,
            "SEPA",
            "MSG-1",
            "E2E-1",
            PartyRole.Creditor,
            partyName,
            "Hybrid",
            "Warning",
            "Rejected",
            "DE",
            null,
            "10115",
            "Hauptstrasse",
            "12",
            null,
            $"batch/{partyName}",
            [new AssessedIssue("Future", "ADDR-STRUCT-001", "TownName", "Error", "Structured town is required")]);

        factory.Validation.Add(
            new ValidationRunSummary(runId, Guid.NewGuid(), sourceCode, "SEPA", "Completed", DateOnly.FromDateTime(DateTime.UtcNow), 1, 1),
            [assessment]);

        await ReadAsync<CaseGenerationDto>(
            await _client.PostAsJsonAsync("/api/v1/remediation/cases/generate", new { runId }, Token));

        var queue = await ReadAsync<PagedResult<CaseListItemDto>>(
            await _client.GetAsync($"/api/v1/remediation/cases?sourceCode={sourceCode}", Token));

        return queue.Items.Single(item => item.PartyName == partyName).Id;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "the request failed: {0}",
            await response.Content.ReadAsStringAsync(Token));

        return (await response.Content.ReadFromJsonAsync<T>(Json, Token))!;
    }
}
