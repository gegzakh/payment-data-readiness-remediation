using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.BuildingBlocks.Core.Paging;
using PDR.Simulation.Application.Scenarios;
using PDR.Simulation.Domain.Scenarios;

namespace PDR.Simulation.IntegrationTests;

/// <summary>
/// Scenarios must produce stored, reconciled and comparable runs, because the numbers in a go/no-go pack
/// are only credible if they can be reproduced later (FR-SIM-001, FR-SIM-002).
/// </summary>
public sealed class SimulationApiTests(SimulationApiFactory factory) : IClassFixture<SimulationApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly HttpClient _client = factory.CreateClientAs("analyst");

    [Fact]
    public async Task The_standard_scenarios_are_seeded_for_a_new_installation()
    {
        var scenarios = await _client.GetFromJsonAsync<List<ScenarioDto>>(
            "/api/v1/simulation/scenarios",
            Json,
            Token);

        scenarios!.Select(scenario => scenario.Code)
            .Should().Contain(["BASE-CURRENT", "BASE-FUTURE", "BASE-REMEDIATED"]);
    }

    [Fact]
    public async Task A_run_stores_a_reconciled_population_with_its_breakdown()
    {
        var run = await RunAsync("BASE-FUTURE");

        var stored = await _client.GetFromJsonAsync<SimulationRunDto>(
            $"/api/v1/simulation/runs/{run.Id}",
            Json,
            Token);

        stored!.Status.Should().Be(RunStatus.Completed);
        stored.PopulationCount.Should().Be(stored.AssessedCount + stored.ExcludedCount + stored.UnableToAssessCount);
        stored.RejectedCount.Should().Be(400);
        stored.RulesetVersion.Should().Be("2026.1");
        stored.RequestedBy.Should().Be("analyst");
        stored.Breakdown.Should().Contain(row => row.Dimension == BreakdownDimension.Scheme && row.Key == "SEPA");
    }

    [Fact]
    public async Task Re_running_the_same_definition_reproduces_the_same_run_key()
    {
        var first = await RunAsync("BASE-CURRENT");
        var second = await RunAsync("BASE-CURRENT");

        second.RunKey.Should().Be(first.RunKey);
        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task Comparing_a_future_run_with_a_remediated_run_shows_the_exposure_that_remediation_removes()
    {
        var future = await RunAsync("BASE-FUTURE");
        var remediated = await RunAsync("BASE-REMEDIATED");

        var comparison = await _client.GetFromJsonAsync<RunComparisonDto>(
            $"/api/v1/simulation/runs/compare?baselineId={future.Id}&candidateId={remediated.Id}",
            Json,
            Token);

        comparison!.RejectedDelta.Should().Be(-200);
        comparison.SameRunKey.Should().BeFalse();
        comparison.ReadinessDelta.Should().BeGreaterThan(0);
        comparison.Rows.Should().NotBeEmpty();
    }

    [Fact]
    public async Task A_scenario_scope_narrows_the_stored_breakdown()
    {
        var created = await _client.PostAsJsonAsync(
            "/api/v1/simulation/scenarios",
            new
            {
                code = "sepa-only",
                name = "SEPA only",
                mode = nameof(ScenarioMode.Future),
                asOf = "2026-11-22",
                schemeCodes = "SEPA"
            },
            Token);

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var run = await RunAsync("SEPA-ONLY");

        run.Breakdown.Where(row => row.Dimension == BreakdownDimension.Scheme)
            .Select(row => row.Key)
            .Should().BeEquivalentTo(["SEPA"]);
    }

    [Fact]
    public async Task A_locked_scenario_cannot_be_edited_but_an_archived_one_cannot_be_run()
    {
        await _client.PostAsJsonAsync(
            "/api/v1/simulation/scenarios",
            new { code = "locked", name = "Locked", mode = nameof(ScenarioMode.Current), asOf = "2026-01-01" },
            Token);

        await _client.PostAsync("/api/v1/simulation/scenarios/locked/lock", null, Token);

        var edit = await _client.PutAsJsonAsync(
            "/api/v1/simulation/scenarios/locked",
            new { name = "Renamed", asOf = "2026-01-01" },
            Token);

        await _client.PostAsync("/api/v1/simulation/scenarios/locked/archive", null, Token);
        var run = await _client.PostAsync("/api/v1/simulation/scenarios/locked/run", null, Token);

        edit.StatusCode.Should().Be(HttpStatusCode.Conflict);
        run.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Runs_are_paged_with_the_configured_page_size()
    {
        await RunAsync("BASE-CURRENT");

        var page = await _client.GetFromJsonAsync<PagedResult<SimulationRunDto>>(
            "/api/v1/simulation/runs?page=1&pageSize=1",
            Json,
            Token);

        page!.Items.Should().ContainSingle();
        page.PageSize.Should().Be(1);
        page.TotalCount.Should().BeGreaterThan(0);
    }

    private async Task<SimulationRunDto> RunAsync(string scenarioCode)
    {
        var response = await _client.PostAsync($"/api/v1/simulation/scenarios/{scenarioCode}/run", null, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<SimulationRunDto>(Json, Token))!;
    }
}
