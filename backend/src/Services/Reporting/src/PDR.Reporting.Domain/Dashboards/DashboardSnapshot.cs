using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;

namespace PDR.Reporting.Domain.Dashboards;

/// <summary>
/// A dashboard as it was at a point in time: its metrics, its drillable rows and the scope, ruleset and
/// upstream freshness they were derived from. Snapshots are immutable so a number quoted in a meeting can
/// be reproduced afterwards (FR-RPT-002).
/// </summary>
public sealed class DashboardSnapshot : AggregateRoot
{
    private readonly List<MetricValue> _metrics = [];
    private readonly List<MetricBreakdown> _breakdown = [];

    private DashboardSnapshot()
    {
    }

    private DashboardSnapshot(
        DashboardAudience audience,
        DashboardScope scope,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset? sourceAsOfUtc,
        string? rulesetVersion,
        ReconciliationStatus reconciliation,
        string? reconciliationNote)
    {
        Audience = audience;
        ScopeKey = Ensure.MaxLength(scope.Key, 512);
        ScopeDescription = Ensure.MaxLength(scope.Description, 512);
        SchemeCodes = scope.SchemeCodes;
        SourceCodes = scope.SourceCodes;
        Countries = scope.Countries;
        Exclusions = scope.Exclusions;
        AsOf = scope.AsOf;
        CapturedAtUtc = capturedAtUtc;
        SourceAsOfUtc = sourceAsOfUtc;
        RulesetVersion = rulesetVersion is null ? null : Ensure.MaxLength(rulesetVersion, 32);
        Reconciliation = reconciliation;
        ReconciliationNote = reconciliationNote is null ? null : Ensure.MaxLength(reconciliationNote, 512);
    }

    public DashboardAudience Audience { get; private set; }

    public string ScopeKey { get; private set; } = string.Empty;

    public string ScopeDescription { get; private set; } = string.Empty;

    public string? SchemeCodes { get; private set; }

    public string? SourceCodes { get; private set; }

    public string? Countries { get; private set; }

    public string? Exclusions { get; private set; }

    public DateOnly? AsOf { get; private set; }

    public DateTimeOffset CapturedAtUtc { get; private set; }

    /// <summary>How old the underlying validation data was when the snapshot was taken (FR-RPT-002).</summary>
    public DateTimeOffset? SourceAsOfUtc { get; private set; }

    public string? RulesetVersion { get; private set; }

    public ReconciliationStatus Reconciliation { get; private set; }

    public string? ReconciliationNote { get; private set; }

    public IReadOnlyCollection<MetricValue> Metrics => _metrics.AsReadOnly();

    public IReadOnlyCollection<MetricBreakdown> Breakdown => _breakdown.AsReadOnly();

    public static DashboardSnapshot Capture(
        DashboardAudience audience,
        DashboardScope scope,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset? sourceAsOfUtc,
        string? rulesetVersion,
        ReconciliationStatus reconciliation,
        string? reconciliationNote = null) =>
        new(audience, scope, capturedAtUtc, sourceAsOfUtc, rulesetVersion, reconciliation, reconciliationNote);

    public DashboardSnapshot AddMetric(
        string key,
        string label,
        decimal value,
        MetricUnit unit = MetricUnit.Count,
        MetricDirection direction = MetricDirection.Neutral,
        string? drillDimension = null,
        string? text = null)
    {
        _metrics.Add(new MetricValue(Id, key, label, value, unit, direction, drillDimension, text));
        return this;
    }

    public DashboardSnapshot AddBreakdown(
        string dimension,
        string key,
        int recordCount,
        int rejectedCount,
        int warningCount,
        int paymentsAtRisk)
    {
        _breakdown.Add(new MetricBreakdown(Id, dimension, key, recordCount, rejectedCount, warningCount, paymentsAtRisk));
        return this;
    }

    /// <summary>A snapshot is reused until it ages past the configured freshness window (FR-RPT-002).</summary>
    public bool IsFreshAt(DateTimeOffset now, TimeSpan window) => now - CapturedAtUtc < window;
}

public sealed class MetricValue : Entity
{
    private MetricValue()
    {
    }

    internal MetricValue(
        Guid snapshotId,
        string key,
        string label,
        decimal value,
        MetricUnit unit,
        MetricDirection direction,
        string? drillDimension,
        string? text)
    {
        SnapshotId = snapshotId;
        Key = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(key), 64);
        Label = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(label), 140);
        Value = value;
        Unit = unit;
        Direction = direction;
        DrillDimension = drillDimension is null ? null : Ensure.MaxLength(drillDimension, 32);
        Text = text is null ? null : Ensure.MaxLength(text, 140);
    }

    public Guid SnapshotId { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    public decimal Value { get; private set; }

    public MetricUnit Unit { get; private set; }

    public MetricDirection Direction { get; private set; }

    /// <summary>The dimension a caller should drill into for this metric, if any (FR-RPT-002).</summary>
    public string? DrillDimension { get; private set; }

    public string? Text { get; private set; }
}

public sealed class MetricBreakdown : Entity
{
    private MetricBreakdown()
    {
    }

    internal MetricBreakdown(
        Guid snapshotId,
        string dimension,
        string key,
        int recordCount,
        int rejectedCount,
        int warningCount,
        int paymentsAtRisk)
    {
        SnapshotId = snapshotId;
        Dimension = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(dimension), 32);
        Key = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(key), 140);
        RecordCount = Math.Max(recordCount, 0);
        RejectedCount = Math.Clamp(rejectedCount, 0, RecordCount);
        WarningCount = Math.Max(warningCount, 0);
        PaymentsAtRisk = Math.Max(paymentsAtRisk, 0);
    }

    public Guid SnapshotId { get; private set; }

    public string Dimension { get; private set; } = string.Empty;

    public string Key { get; private set; } = string.Empty;

    public int RecordCount { get; private set; }

    public int RejectedCount { get; private set; }

    public int WarningCount { get; private set; }

    public int PaymentsAtRisk { get; private set; }

    public decimal ReadinessPercent =>
        RecordCount == 0 ? 0m : Math.Round((RecordCount - RejectedCount) * 100m / RecordCount, 2);
}

public static class DashboardErrors
{
    public static Error NotFound(DashboardAudience audience) =>
        Error.NotFound("REPORTING.DASHBOARD_NOT_FOUND", $"No snapshot has been captured for the '{audience}' dashboard.");

    public static Error UnknownDimension(string dimension) =>
        Error.Validation("REPORTING.UNKNOWN_DIMENSION", $"'{dimension}' is not a dimension this dashboard can be drilled by.");
}
