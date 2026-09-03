using PDR.Reporting.Domain.Dashboards;

namespace PDR.Reporting.Application.Dashboards;

public sealed record MetricDto(
    string Key,
    string Label,
    decimal Value,
    MetricUnit Unit,
    MetricDirection Direction,
    string? DrillDimension,
    string? Text);

public sealed record BreakdownRowDto(
    string Dimension,
    string Key,
    int RecordCount,
    int RejectedCount,
    int WarningCount,
    int PaymentsAtRisk,
    decimal ReadinessPercent);

public sealed record DashboardDto(
    Guid Id,
    DashboardAudience Audience,
    string ScopeKey,
    string ScopeDescription,
    string? SchemeCodes,
    string? SourceCodes,
    string? Countries,
    string? Exclusions,
    DateOnly? AsOf,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset? SourceAsOfUtc,
    string? RulesetVersion,
    ReconciliationStatus Reconciliation,
    string? ReconciliationNote,
    IReadOnlyList<MetricDto> Metrics,
    IReadOnlyList<BreakdownRowDto> Breakdown);

public sealed record DrillDownDto(
    DashboardAudience Audience,
    string Dimension,
    string ScopeDescription,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset? SourceAsOfUtc,
    string? RulesetVersion,
    ReconciliationStatus Reconciliation,
    IReadOnlyList<BreakdownRowDto> Rows);

public static class ReportingSettingKeys
{
    /// <summary>How long a captured snapshot may be reused before it is rebuilt (FR-RPT-002).</summary>
    public const string FreshnessSeconds = "reporting.freshness-seconds";

    public const string HistoryPageSize = "reporting.history-page-size";
}

public static class ReportingDefaults
{
    public const int FreshnessSeconds = 300;
    public const int HistoryPageSize = 20;
    public const int MaxPageSize = 200;
}

public static class ReportingMapper
{
    public static DashboardDto ToDto(this DashboardSnapshot snapshot) =>
        new(
            snapshot.Id,
            snapshot.Audience,
            snapshot.ScopeKey,
            snapshot.ScopeDescription,
            snapshot.SchemeCodes,
            snapshot.SourceCodes,
            snapshot.Countries,
            snapshot.Exclusions,
            snapshot.AsOf,
            snapshot.CapturedAtUtc,
            snapshot.SourceAsOfUtc,
            snapshot.RulesetVersion,
            snapshot.Reconciliation,
            snapshot.ReconciliationNote,
            [.. snapshot.Metrics.Select(metric => new MetricDto(
                metric.Key,
                metric.Label,
                metric.Value,
                metric.Unit,
                metric.Direction,
                metric.DrillDimension,
                metric.Text))],
            [.. snapshot.Breakdown
                .OrderByDescending(row => row.RejectedCount)
                .ThenBy(row => row.Key, StringComparer.Ordinal)
                .Select(row => row.ToDto())]);

    public static BreakdownRowDto ToDto(this MetricBreakdown row) =>
        new(row.Dimension, row.Key, row.RecordCount, row.RejectedCount, row.WarningCount, row.PaymentsAtRisk, row.ReadinessPercent);
}
