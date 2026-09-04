using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace PDR.Simulation.IntegrationTests;

/// <summary>
/// Simulation results and the go/no-go pack drive a production cutover decision, so nothing may be read
/// or written without an authenticated caller holding the matching permission (FR-SEC-001).
/// </summary>
public sealed class SimulationSecurityTests(SecuredSimulationApiFactory factory)
    : IClassFixture<SecuredSimulationApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public static TheoryData<string> ProtectedReads =>
    [
        "/api/v1/simulation/scenarios",
        "/api/v1/simulation/scenarios/BASE-CURRENT",
        "/api/v1/simulation/runs",
        $"/api/v1/simulation/runs/{Guid.Empty}",
        $"/api/v1/simulation/runs/compare?baselineId={Guid.Empty}&candidateId={Guid.Empty}",
        "/api/v1/simulation/test-plans",
        "/api/v1/simulation/cutover",
        "/api/v1/simulation/cutover/CUTOVER-2026",
        "/api/v1/simulation/cutover/CUTOVER-2026/go-no-go"
    ];

    [Theory]
    [MemberData(nameof(ProtectedReads))]
    public async Task No_simulation_data_is_readable_anonymously(string path)
    {
        var response = await _client.GetAsync(path, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Running_a_scenario_and_signing_off_a_cutover_reject_anonymous_callers()
    {
        var run = await _client.PostAsync("/api/v1/simulation/scenarios/BASE-FUTURE/run", null, Token);
        var criterion = await _client.PostAsJsonAsync(
            "/api/v1/simulation/cutover/CUTOVER-2026/criteria/ENTRY-READINESS/status",
            new { status = "Met", evidenceReference = "evidence://x" },
            Token);
        var approval = await _client.PostAsJsonAsync(
            "/api/v1/simulation/cutover/CUTOVER-2026/approvals",
            new { role = "Programme", decision = "Approved", rationale = "Go" },
            Token);

        run.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        criterion.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        approval.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_and_api_documentation_stay_reachable_for_operations()
    {
        var health = await _client.GetAsync("/health/live", Token);

        health.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
