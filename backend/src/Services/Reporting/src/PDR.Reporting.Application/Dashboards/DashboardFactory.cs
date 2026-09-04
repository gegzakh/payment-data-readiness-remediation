using PDR.BuildingBlocks.Core.Time;
using PDR.Reporting.Application.Upstream;
using PDR.Reporting.Domain.Dashboards;

namespace PDR.Reporting.Application.Dashboards;

/// <summary>
/// Builds each audience's dashboard from the upstream services and stamps the result with the scope,
/// ruleset and data freshness it was derived from, so a figure can always be traced back (FR-RPT-001,
/// FR-RPT-002). Composition lives here rather than in handlers so the metric arithmetic is unit testable.
/// </summary>
public sealed class DashboardFactory(
    IValidationGateway validation,
    IRemediationGateway remediation,
    ISimulationGateway simulation,
    IClock clock)
{
    public static readonly IReadOnlyList<string> Dimensions = ["Scheme", "Source", "Country", "Issue"];

    public async Task<DashboardSnapshot> BuildAsync(
        DashboardAudience audience,
        DashboardScope scope,
        CancellationToken cancellationToken)
    {
        var portfolio = await validation.GetSnapshotAsync(cancellationToken);
        var cases = await remediation.GetSnapshotAsync(cancellationToken);
        var lab = await simulation.GetSnapshotAsync(cancellationToken);

        var missing = new List<string>();
        if (portfolio == ValidationSnapshot.Empty)
        {
            missing.Add("validation");
        }

        if (cases == RemediationSnapshot.Empty)
        {
            missing.Add("remediation");
        }

        if (lab == SimulationSnapshot.Empty)
        {
            missing.Add("simulation");
        }

        var reconciliation = missing.Count switch
        {
            0 => ReconciliationStatus.Reconciled,
            3 => ReconciliationStatus.Unreconciled,
            _ => ReconciliationStatus.Partial
        };

        var snapshot = DashboardSnapshot.Capture(
            audience,
            scope,
            clock.UtcNow,
            portfolio == ValidationSnapshot.Empty ? null : portfolio.AsOfUtc,
            portfolio.RulesetVersion ?? lab.RulesetVersion,
            reconciliation,
            missing.Count == 0 ? null : $"No data from {string.Join(", ", missing)}.");

        switch (audience)
        {
            case DashboardAudience.Executive:
                Executive(snapshot, portfolio, cases, lab);
                break;
            case DashboardAudience.Scheme:
                await DimensionAsync(snapshot, scope, "Scheme", cancellationToken);
                break;
            case DashboardAudience.Source:
                await DimensionAsync(snapshot, scope, "Source", cancellationToken);
                break;
            case DashboardAudience.Operations:
                await OperationsAsync(snapshot, scope, portfolio, cancellationToken);
                break;
            case DashboardAudience.Remediation:
                Remediation(snapshot, cases);
                break;
            case DashboardAudience.Testing:
                Testing(snapshot, lab);
                break;
            default:
                Cutover(snapshot, lab);
                break;
        }

        return snapshot;
    }

    private static void Executive(
        DashboardSnapshot snapshot,
        ValidationSnapshot portfolio,
        RemediationSnapshot cases,
        SimulationSnapshot lab)
    {
        snapshot
            .AddMetric("population", "Parties assessed", portfolio.AssessedCount, MetricUnit.Count)
            .AddMetric("current-readiness", "Readiness under current rules", portfolio.CurrentReadinessPercent, MetricUnit.Percent, MetricDirection.HigherIsBetter)
            .AddMetric("future-readiness", "Readiness under future rules", portfolio.FutureReadinessPercent, MetricUnit.Percent, MetricDirection.HigherIsBetter, "Scheme")
            .AddMetric("payments-at-risk", "Payments at risk", portfolio.PaymentsAtRisk, MetricUnit.Count, MetricDirection.LowerIsBetter, "Scheme")
            .AddMetric("residual-exposure", "Residual exposure after remediation", lab.ResidualExposure, MetricUnit.Count, MetricDirection.LowerIsBetter)
            .AddMetric("open-cases", "Open remediation cases", cases.OpenCases, MetricUnit.Count, MetricDirection.LowerIsBetter)
            .AddMetric("expired-exceptions", "Expired exceptions", cases.ExpiredExceptions, MetricUnit.Count, MetricDirection.LowerIsBetter)
            .AddMetric("recommendation", "Go / no-go", 0, MetricUnit.Text, MetricDirection.Neutral, null, lab.Recommendation ?? "Not assessed");
    }

    private async Task DimensionAsync(
        DashboardSnapshot snapshot,
        DashboardScope scope,
        string dimension,
        CancellationToken cancellationToken)
    {
        var rows = await ScopedRowsAsync(scope, dimension, cancellationToken);

        var records = rows.Sum(row => row.RecordCount);
        var rejected = rows.Sum(row => row.FutureRejectedCount);

        snapshot
            .AddMetric($"{dimension.ToLowerInvariant()}-count", $"{dimension}s in scope", rows.Count, MetricUnit.Count)
            .AddMetric("population", "Parties in scope", records, MetricUnit.Count)
            .AddMetric("future-rejected", "Rejected under future rules", rejected, MetricUnit.Count, MetricDirection.LowerIsBetter, dimension)
            .AddMetric(
                "future-readiness",
                "Readiness under future rules",
                records == 0 ? 0m : Math.Round((records - rejected) * 100m / records, 2),
                MetricUnit.Percent,
                MetricDirection.HigherIsBetter,
                dimension);

        foreach (var row in rows)
        {
            snapshot.AddBreakdown(dimension, row.Key, row.RecordCount, row.FutureRejectedCount, row.FutureWarningCount, row.FutureRejectedCount);
        }
    }

    private async Task OperationsAsync(
        DashboardSnapshot snapshot,
        DashboardScope scope,
        ValidationSnapshot portfolio,
        CancellationToken cancellationToken)
    {
        var issues = await ScopedRowsAsync(scope, "Issue", cancellationToken);

        snapshot
            .AddMetric("population", "Parties assessed", portfolio.AssessedCount, MetricUnit.Count)
            .AddMetric("unable-to-assess", "Unable to assess", portfolio.UnableToAssessCount, MetricUnit.Count, MetricDirection.LowerIsBetter)
            .AddMetric("excluded", "Excluded from scope", portfolio.ExcludedCount, MetricUnit.Count)
            .AddMetric("current-warnings", "Warnings under current rules", portfolio.CurrentWarningCount, MetricUnit.Count, MetricDirection.LowerIsBetter, "Issue")
            .AddMetric("top-issue", "Most common issue", 0, MetricUnit.Text, MetricDirection.Neutral, "Issue", issues.Count == 0 ? "None" : issues[0].Key);

        foreach (var row in issues)
        {
            snapshot.AddBreakdown("Issue", row.Key, row.RecordCount, row.FutureRejectedCount, row.FutureWarningCount, row.FutureRejectedCount);
        }
    }

    private static void Remediation(DashboardSnapshot snapshot, RemediationSnapshot cases)
    {
        snapshot
            .AddMetric("total-cases", "Cases raised", cases.TotalCases, MetricUnit.Count)
            .AddMetric("open-cases", "Open", cases.OpenCases, MetricUnit.Count, MetricDirection.LowerIsBetter)
            .AddMetric("approved-cases", "Approved awaiting write-back", cases.ApprovedCases, MetricUnit.Count)
            .AddMetric("remediated-cases", "Written back", cases.RemediatedCases, MetricUnit.Count, MetricDirection.HigherIsBetter)
            .AddMetric("completion", "Completion", cases.CompletionPercent, MetricUnit.Percent, MetricDirection.HigherIsBetter)
            .AddMetric("expired-exceptions", "Expired exceptions", cases.ExpiredExceptions, MetricUnit.Count, MetricDirection.LowerIsBetter)
            .AddMetric("exposure-remaining", "Exposure still open", cases.FutureExposureOpen, MetricUnit.Count, MetricDirection.LowerIsBetter);
    }

    private static void Testing(DashboardSnapshot snapshot, SimulationSnapshot lab)
    {
        snapshot
            .AddMetric("coverage", "Risk-weighted coverage", lab.TestCoveragePercent, MetricUnit.Percent, MetricDirection.HigherIsBetter)
            .AddMetric("open-defects", "Open defects", lab.OpenDefects, MetricUnit.Count, MetricDirection.LowerIsBetter)
            .AddMetric("uat-mismatches", "UAT reconciliation mismatches", lab.UatMismatches, MetricUnit.Count, MetricDirection.LowerIsBetter);
    }

    private static void Cutover(DashboardSnapshot snapshot, SimulationSnapshot lab)
    {
        snapshot
            .AddMetric("recommendation", "Go / no-go", 0, MetricUnit.Text, MetricDirection.Neutral, null, lab.Recommendation ?? "Not assessed")
            .AddMetric("residual-exposure", "Residual exposure", lab.ResidualExposure, MetricUnit.Count, MetricDirection.LowerIsBetter)
            .AddMetric("entry-outstanding", "Entry criteria outstanding", lab.EntryCriteriaOutstanding, MetricUnit.Count, MetricDirection.LowerIsBetter)
            .AddMetric("exit-outstanding", "Exit criteria outstanding", lab.ExitCriteriaOutstanding, MetricUnit.Count, MetricDirection.LowerIsBetter)
            .AddMetric("waived", "Waived criteria", lab.WaivedCriteria, MetricUnit.Count, MetricDirection.LowerIsBetter)
            .AddMetric("simulated-readiness", "Readiness in the remediated simulation", lab.RemediatedReadinessPercent, MetricUnit.Percent, MetricDirection.HigherIsBetter);
    }

    private async Task<IReadOnlyList<ValidationProfileRow>> ScopedRowsAsync(
        DashboardScope scope,
        string dimension,
        CancellationToken cancellationToken)
    {
        var rows = await validation.GetProfileAsync(dimension, cancellationToken);

        return
        [
            .. rows
                .Where(row => scope.Includes(dimension, row.Key))
                .OrderByDescending(row => row.FutureRejectedCount)
                .ThenBy(row => row.Key, StringComparer.Ordinal)
        ];
    }
}
