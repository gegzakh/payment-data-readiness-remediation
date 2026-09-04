using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.Simulation.Application.Testing;
using PDR.Simulation.Domain.Testing;

namespace PDR.Simulation.IntegrationTests;

/// <summary>Risk-based execution, defects, retests and UAT reconciliation (FR-TST-001, FR-TST-003).</summary>
public sealed class TestPlanApiTests(SimulationApiFactory factory) : IClassFixture<SimulationApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly HttpClient _client = factory.CreateClientAs("tester");

    [Fact]
    public async Task A_plan_cannot_be_activated_before_it_has_cases()
    {
        var code = await CreatePlanAsync("empty");

        var activate = await _client.PostAsync($"/api/v1/simulation/test-plans/{code}/activate", null, Token);

        activate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_failed_execution_without_a_defect_is_rejected()
    {
        var code = await CreatePlanAsync("defect");
        await AddCaseAsync(code, "TC-1", TestRisk.Critical);
        await _client.PostAsync($"/api/v1/simulation/test-plans/{code}/activate", null, Token);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/simulation/test-plans/{code}/cases/TC-1/execution",
            new { status = nameof(TestExecutionStatus.Failed), actualResult = "Rejected" },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_plan_closes_only_after_a_failed_case_passes_a_retest()
    {
        var code = await CreatePlanAsync("retest");
        await AddCaseAsync(code, "TC-1", TestRisk.High);
        await _client.PostAsync($"/api/v1/simulation/test-plans/{code}/activate", null, Token);

        await ExecuteAsync(code, "TC-1", TestExecutionStatus.Failed, "Rejected", "JIRA-9");
        var earlyClose = await _client.PostAsync($"/api/v1/simulation/test-plans/{code}/close", null, Token);

        var retested = await ExecuteAsync(code, "TC-1", TestExecutionStatus.Passed, "Accepted after fix", null);
        var close = await _client.PostAsync($"/api/v1/simulation/test-plans/{code}/close", null, Token);

        earlyClose.StatusCode.Should().Be(HttpStatusCode.Conflict);
        close.StatusCode.Should().Be(HttpStatusCode.OK);

        var plan = (await retested.Content.ReadFromJsonAsync<TestPlanDto>(Json, Token))!;
        plan.OpenDefectCount.Should().Be(0);
        plan.Cases.Single().ExecutionCount.Should().Be(2);
        plan.Cases.Single().DefectReference.Should().Be("JIRA-9");
        plan.Cases.Single().ExecutedBy.Should().Be("tester");
    }

    [Fact]
    public async Task Uat_reconciliation_records_a_mismatch_between_the_engine_and_the_platform()
    {
        var code = await CreatePlanAsync("uat");
        await AddCaseAsync(code, "TC-1", TestRisk.Medium);
        await _client.PostAsync($"/api/v1/simulation/test-plans/{code}/activate", null, Token);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/simulation/test-plans/{code}/cases/TC-1/uat",
            new
            {
                engineOutcome = "Rejected",
                platformOutcome = "Accepted",
                explanation = "The engine applies a stricter town rule."
            },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var plan = (await response.Content.ReadFromJsonAsync<TestPlanDto>(Json, Token))!;
        plan.Cases.Single().UatOutcome.Should().Be(UatOutcome.Mismatch);
        plan.UatMismatchCount.Should().Be(1);
    }

    private async Task<string> CreatePlanAsync(string suffix)
    {
        var code = $"PLAN-{suffix}".ToUpperInvariant();

        var created = await _client.PostAsJsonAsync(
            "/api/v1/simulation/test-plans",
            new { code, name = $"Plan {suffix}", owner = "Test Manager", scope = "SEPA samples" },
            Token);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        return code;
    }

    private async Task AddCaseAsync(string code, string reference, TestRisk risk) =>
        await _client.PostAsJsonAsync(
            $"/api/v1/simulation/test-plans/{code}/cases",
            new
            {
                reference,
                title = "Structured address accepted",
                risk = risk.ToString(),
                scenarioCode = "BASE-FUTURE",
                sampleReference = "SAMPLE-1",
                expectedResult = "Accepted"
            },
            Token);

    private Task<HttpResponseMessage> ExecuteAsync(
        string code,
        string reference,
        TestExecutionStatus status,
        string actualResult,
        string? defectReference) =>
        _client.PostAsJsonAsync(
            $"/api/v1/simulation/test-plans/{code}/cases/{reference}/execution",
            new
            {
                status = status.ToString(),
                actualResult,
                evidenceReference = "evidence://uat/1",
                defectReference
            },
            Token);
}
