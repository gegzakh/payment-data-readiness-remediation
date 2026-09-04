using AwesomeAssertions;
using PDR.BuildingBlocks.Core.Time;
using PDR.Simulation.Application.Scenarios;
using PDR.Simulation.Application.Upstream;
using PDR.Simulation.Domain.Scenarios;

namespace PDR.Simulation.UnitTests;

public sealed class SimulationRunnerTests
{
    private static readonly DateOnly AsOf = new(2026, 11, 22);

    private static readonly PortfolioSnapshot Portfolio = new(
        AssessedCount: 1000,
        ExcludedCount: 50,
        UnableToAssessCount: 25,
        CurrentRejectedCount: 120,
        FutureRejectedCount: 400,
        PaymentsAtRisk: 380,
        RulesetVersion: "2026.1",
        AsOfUtc: new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));

    private static readonly RemediationSnapshot Remediation = new(
        TotalCases: 400,
        RemediatedCases: 150,
        ApprovedCases: 50,
        OpenCases: 200,
        ExpiredExceptions: 0,
        FutureExposureOpen: 190,
        FutureExposureRemediated: 190);

    [Fact]
    public async Task Current_run_uses_todays_rejections_and_reconciles_the_population()
    {
        var run = await RunAsync(ScenarioMode.Current);

        run.RejectedCount.Should().Be(120);
        run.PopulationCount.Should().Be(1075);
        run.Reconciles.Should().BeTrue();
        run.ReadinessPercent.Should().Be(88.00m);
        run.RulesetVersion.Should().Be("2026.1");
    }

    [Fact]
    public async Task Future_run_uses_the_post_cutover_rejections_and_exposure()
    {
        var run = await RunAsync(ScenarioMode.Future);

        run.RejectedCount.Should().Be(400);
        run.PaymentsAtRisk.Should().Be(380);
    }

    [Fact]
    public async Task Remediated_run_removes_what_remediation_has_approved_or_written_back()
    {
        var run = await RunAsync(ScenarioMode.Remediated);

        run.RejectedCount.Should().Be(200);
        run.PaymentsAtRisk.Should().Be(190);
        run.RejectedCount.Should().BeLessThan(400);
    }

    [Fact]
    public async Task Scope_filters_keep_only_the_dimensions_in_scope()
    {
        var scenario = Scenario.Create(
            "SEPA-ONLY",
            "SEPA only",
            ScenarioMode.Future,
            AsOf,
            schemeCodes: "SEPA");

        var run = await ExecuteAsync(scenario);

        run.Breakdown.Where(row => row.Dimension == BreakdownDimension.Scheme)
            .Select(row => row.Key)
            .Should().BeEquivalentTo(["SEPA"]);
    }

    [Fact]
    public async Task Archived_scenario_is_refused()
    {
        var scenario = Scenario.Create("OLD", "Old", ScenarioMode.Future, AsOf);
        scenario.Archive();

        var runner = new SimulationRunner(new FakePortfolio(), new FakeRemediation(), new FakeClock());

        var result = await runner.ExecuteAsync(scenario, "tester", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SCENARIO.ARCHIVED");
    }

    private static Task<SimulationRun> RunAsync(ScenarioMode mode) =>
        ExecuteAsync(Scenario.Create(mode.ToString().ToUpperInvariant(), mode.ToString(), mode, AsOf));

    private static async Task<SimulationRun> ExecuteAsync(Scenario scenario)
    {
        var runner = new SimulationRunner(new FakePortfolio(), new FakeRemediation(), new FakeClock());

        var result = await runner.ExecuteAsync(scenario, "tester", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private sealed class FakePortfolio : IPortfolioGateway
    {
        public Task<PortfolioSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Portfolio);

        public Task<IReadOnlyList<PortfolioProfileRow>> GetProfileAsync(
            string dimension,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PortfolioProfileRow>>(dimension switch
            {
                "Scheme" =>
                [
                    new PortfolioProfileRow("Scheme", "SEPA", 600, 60, 240, 10, 30),
                    new PortfolioProfileRow("Scheme", "SWIFT", 400, 60, 160, 5, 20)
                ],
                "Source" => [new PortfolioProfileRow("Source", "CBS", 1000, 120, 400, 15, 50)],
                "Country" => [new PortfolioProfileRow("Country", "DE", 1000, 120, 400, 15, 50)],
                _ => [new PortfolioProfileRow("Issue", "MISSING_TOWN", 1000, 120, 400, 15, 50)]
            });
    }

    private sealed class FakeRemediation : IRemediationGateway
    {
        public Task<RemediationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Remediation);
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);
    }
}
