using AwesomeAssertions;
using PDR.BuildingBlocks.Core.Time;
using PDR.Reporting.Application.Dashboards;
using PDR.Reporting.Application.Upstream;
using PDR.Reporting.Domain.Dashboards;

namespace PDR.Reporting.UnitTests;

public sealed class DashboardFactoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly ValidationSnapshot Portfolio = new(
        AssessedCount: 1000,
        ExcludedCount: 40,
        UnableToAssessCount: 10,
        CurrentRejectedCount: 100,
        FutureRejectedCount: 250,
        CurrentWarningCount: 30,
        FutureWarningCount: 60,
        PaymentsAtRisk: 240,
        RulesetVersion: "2026.1",
        AsOfUtc: Now.AddMinutes(-5));

    private static readonly RemediationSnapshot Cases = new(
        TotalCases: 200,
        OpenCases: 80,
        ApprovedCases: 20,
        RemediatedCases: 100,
        ExpiredExceptions: 3,
        FutureExposureOpen: 90,
        FutureExposureRemediated: 150);

    private static readonly SimulationSnapshot Lab = new(
        LatestRunId: Guid.CreateVersion7(),
        LatestRunScenario: "REMEDIATED",
        LatestRunAtUtc: Now.AddHours(-1),
        RemediatedRejectedCount: 60,
        RemediatedPaymentsAtRisk: 55,
        RemediatedReadinessPercent: 94m,
        Recommendation: "Go",
        ResidualExposure: 55,
        EntryCriteriaOutstanding: 1,
        ExitCriteriaOutstanding: 2,
        WaivedCriteria: 1,
        OpenDefects: 4,
        UatMismatches: 2,
        TestCoveragePercent: 88.5m,
        RulesetVersion: "2026.1");

    [Fact]
    public async Task Executive_dashboard_reports_readiness_and_exposure_from_the_upstreams()
    {
        var snapshot = await BuildAsync(DashboardAudience.Executive, DashboardScope.All);

        Metric(snapshot, "population").Should().Be(1000m);
        Metric(snapshot, "current-readiness").Should().Be(90m);
        Metric(snapshot, "future-readiness").Should().Be(75m);
        Metric(snapshot, "payments-at-risk").Should().Be(240m);
        Metric(snapshot, "residual-exposure").Should().Be(55m);
        snapshot.Metrics.Single(metric => metric.Key == "recommendation").Text.Should().Be("Go");
        snapshot.RulesetVersion.Should().Be("2026.1");
        snapshot.SourceAsOfUtc.Should().Be(Portfolio.AsOfUtc);
    }

    [Fact]
    public async Task Reconciliation_is_partial_when_an_upstream_has_no_data()
    {
        var snapshot = await BuildAsync(
            DashboardAudience.Executive,
            DashboardScope.All,
            simulation: SimulationSnapshot.Empty);

        snapshot.Reconciliation.Should().Be(ReconciliationStatus.Partial);
        snapshot.ReconciliationNote.Should().Contain("simulation");
    }

    [Fact]
    public async Task Reconciliation_is_unreconciled_when_nothing_upstream_answers()
    {
        var snapshot = await BuildAsync(
            DashboardAudience.Executive,
            DashboardScope.All,
            validation: ValidationSnapshot.Empty,
            remediation: RemediationSnapshot.Empty,
            simulation: SimulationSnapshot.Empty);

        snapshot.Reconciliation.Should().Be(ReconciliationStatus.Unreconciled);
    }

    [Fact]
    public async Task Scheme_dashboard_only_counts_rows_inside_the_scope()
    {
        var snapshot = await BuildAsync(
            DashboardAudience.Scheme,
            DashboardScope.Create("SEPA", null, null, null, null));

        snapshot.Breakdown.Should().ContainSingle().Which.Key.Should().Be("SEPA");
        Metric(snapshot, "population").Should().Be(600m);
        Metric(snapshot, "future-rejected").Should().Be(240m);
        Metric(snapshot, "future-readiness").Should().Be(60m);
    }

    [Fact]
    public async Task Breakdown_rows_carry_their_own_readiness()
    {
        var snapshot = await BuildAsync(DashboardAudience.Scheme, DashboardScope.All);

        var sepa = snapshot.Breakdown.Single(row => row.Key == "SEPA");
        sepa.ReadinessPercent.Should().Be(60m);
    }

    [Fact]
    public async Task Cutover_dashboard_surfaces_outstanding_criteria_and_the_recommendation()
    {
        var snapshot = await BuildAsync(DashboardAudience.Cutover, DashboardScope.All);

        Metric(snapshot, "entry-outstanding").Should().Be(1m);
        Metric(snapshot, "exit-outstanding").Should().Be(2m);
        Metric(snapshot, "simulated-readiness").Should().Be(94m);
        snapshot.Metrics.Single(metric => metric.Key == "recommendation").Text.Should().Be("Go");
    }

    [Fact]
    public async Task Snapshot_is_stale_once_the_freshness_window_has_passed()
    {
        var snapshot = await BuildAsync(DashboardAudience.Executive, DashboardScope.All);

        snapshot.IsFreshAt(Now.AddSeconds(30), TimeSpan.FromSeconds(60)).Should().BeTrue();
        snapshot.IsFreshAt(Now.AddSeconds(90), TimeSpan.FromSeconds(60)).Should().BeFalse();
    }

    private static decimal Metric(DashboardSnapshot snapshot, string key) =>
        snapshot.Metrics.Single(metric => metric.Key == key).Value;

    private static Task<DashboardSnapshot> BuildAsync(
        DashboardAudience audience,
        DashboardScope scope,
        ValidationSnapshot? validation = null,
        RemediationSnapshot? remediation = null,
        SimulationSnapshot? simulation = null)
    {
        var factory = new DashboardFactory(
            new FakeValidationGateway(validation ?? Portfolio),
            new FakeRemediationGateway(remediation ?? Cases),
            new FakeSimulationGateway(simulation ?? Lab),
            new FakeClock(Now));

        return factory.BuildAsync(audience, scope, CancellationToken.None);
    }

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class FakeValidationGateway(ValidationSnapshot snapshot) : IValidationGateway
    {
        public Task<ValidationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot);

        public Task<IReadOnlyList<ValidationProfileRow>> GetProfileAsync(
            string dimension,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ValidationProfileRow>>(dimension switch
            {
                "Scheme" =>
                [
                    new ValidationProfileRow("Scheme", "SEPA", 600, 60, 240, 20, 40),
                    new ValidationProfileRow("Scheme", "SWIFT", 400, 40, 10, 10, 20)
                ],
                "Issue" => [new ValidationProfileRow("Issue", "MISSING_TOWN", 1000, 100, 250, 30, 60)],
                _ => []
            });
    }

    private sealed class FakeRemediationGateway(RemediationSnapshot snapshot) : IRemediationGateway
    {
        public Task<RemediationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot);
    }

    private sealed class FakeSimulationGateway(SimulationSnapshot snapshot) : ISimulationGateway
    {
        public Task<SimulationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot);
    }
}
