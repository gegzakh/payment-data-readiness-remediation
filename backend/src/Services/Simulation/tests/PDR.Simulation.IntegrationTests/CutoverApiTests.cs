using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.Simulation.Application.Cutover;
using PDR.Simulation.Application.Testing;
using PDR.Simulation.Domain.Cutover;
using PDR.Simulation.Domain.Testing;

namespace PDR.Simulation.IntegrationTests;

/// <summary>
/// The go/no-go pack has to be derived from evidence — criteria, testing and remediation exposure —
/// rather than asserted by whoever is presenting it (FR-CUT-002, FR-CUT-004).
/// </summary>
public sealed class CutoverApiTests(SimulationApiFactory factory) : IClassFixture<SimulationApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly HttpClient _client = factory.CreateClientAs("programme");

    [Fact]
    public async Task The_default_cutover_checklist_is_seeded_with_entry_and_exit_criteria()
    {
        var plan = await _client.GetFromJsonAsync<CutoverPlanDto>(
            "/api/v1/simulation/cutover/CUTOVER-2026",
            Json,
            Token);

        plan!.Criteria.Should().Contain(criterion => criterion.Kind == CriterionKind.Entry);
        plan.Criteria.Should().Contain(criterion => criterion.Kind == CriterionKind.Exit);
        plan.FallbackPlan.Should().NotBeNullOrWhiteSpace();
        plan.SupportModel.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task A_criterion_marked_met_without_evidence_is_rejected()
    {
        var code = await CreatePlanAsync("evidence");

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/simulation/cutover/{code}/criteria/ENTRY-1/status",
            new { status = nameof(CriterionStatus.Met) },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Residual_exposure_from_the_latest_remediated_run_holds_the_pack_at_no_go()
    {
        var code = await CreatePlanAsync("exposure");
        await MeetCriteriaAsync(code);
        await _client.PostAsync("/api/v1/simulation/scenarios/BASE-REMEDIATED/run", null, Token);

        var pack = await GetPackAsync(code);

        pack.ResidualExposure.Should().Be(200);
        pack.Recommendation.Should().Be(GoNoGoRecommendation.NoGo);
        pack.BasedOnRunId.Should().NotBeNull();

        var approval = await _client.PostAsJsonAsync(
            $"/api/v1/simulation/cutover/{code}/approvals",
            new { role = "Programme", decision = nameof(ApprovalDecision.Approved), rationale = "Ship it" },
            Token);

        approval.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Open_defects_from_the_test_plans_appear_in_the_pack()
    {
        var code = await CreatePlanAsync("defects");
        await MeetCriteriaAsync(code);
        await CreateFailingTestPlanAsync("PACK-DEFECTS");

        var pack = await GetPackAsync(code);

        pack.OpenDefects.Should().BeGreaterThan(0);
        pack.TestCoveragePercent.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task A_pack_with_nothing_outstanding_is_approvable_and_records_the_recommendation_seen()
    {
        var code = await CreatePlanAsync("clean");
        await MeetCriteriaAsync(code);

        var factoryPortfolio = factory.Portfolio.Snapshot;
        factory.Portfolio.Snapshot = factoryPortfolio with { FutureRejectedCount = 0, PaymentsAtRisk = 0 };

        try
        {
            await _client.PostAsync("/api/v1/simulation/scenarios/BASE-REMEDIATED/run", null, Token);

            var pack = await GetPackAsync(code);
            pack.ResidualExposure.Should().Be(0);

            var approval = await _client.PostAsJsonAsync(
                $"/api/v1/simulation/cutover/{code}/approvals",
                new { role = "Programme", decision = nameof(ApprovalDecision.Approved), rationale = "All criteria met" },
                Token);

            approval.StatusCode.Should().Be(HttpStatusCode.OK);

            var plan = (await approval.Content.ReadFromJsonAsync<CutoverPlanDto>(Json, Token))!;
            plan.Approvals.Should().ContainSingle();
            plan.Approvals.Single().Approver.Should().Be("programme");
            plan.Approvals.Single().RecommendationAtSignOff.Should().Be(pack.Recommendation);
        }
        finally
        {
            factory.Portfolio.Snapshot = factoryPortfolio;
        }
    }

    private async Task<GoNoGoPackDto> GetPackAsync(string code) =>
        (await _client.GetFromJsonAsync<GoNoGoPackDto>(
            $"/api/v1/simulation/cutover/{code}/go-no-go",
            Json,
            Token))!;

    private async Task<string> CreatePlanAsync(string suffix)
    {
        var code = $"CUT-{suffix}".ToUpperInvariant();

        var created = await _client.PostAsJsonAsync(
            "/api/v1/simulation/cutover",
            new { code, name = $"Cutover {suffix}", cutoverDate = "2026-11-22", owner = "Programme" },
            Token);

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        await _client.PostAsJsonAsync(
            $"/api/v1/simulation/cutover/{code}/criteria",
            new
            {
                reference = "ENTRY-1",
                kind = nameof(CriterionKind.Entry),
                description = "Readiness above threshold",
                owner = "Data Quality",
                isBlocking = true
            },
            Token);

        return code;
    }

    private async Task MeetCriteriaAsync(string code) =>
        await _client.PostAsJsonAsync(
            $"/api/v1/simulation/cutover/{code}/criteria/ENTRY-1/status",
            new { status = nameof(CriterionStatus.Met), evidenceReference = "evidence://readiness/1" },
            Token);

    private async Task CreateFailingTestPlanAsync(string code)
    {
        await _client.PostAsJsonAsync(
            "/api/v1/simulation/test-plans",
            new { code, name = "UAT", owner = "Test Manager", scope = "SEPA" },
            Token);

        await _client.PostAsJsonAsync(
            $"/api/v1/simulation/test-plans/{code}/cases",
            new
            {
                reference = "TC-1",
                title = "Structured address accepted",
                risk = nameof(TestRisk.High),
                expectedResult = "Accepted"
            },
            Token);

        await _client.PostAsync($"/api/v1/simulation/test-plans/{code}/activate", null, Token);

        await _client.PostAsJsonAsync(
            $"/api/v1/simulation/test-plans/{code}/cases/TC-1/execution",
            new
            {
                status = nameof(TestExecutionStatus.Failed),
                actualResult = "Rejected by the engine",
                defectReference = "JIRA-1"
            },
            Token);
    }
}
