namespace PDR.Reporting.Domain.Dashboards;

/// <summary>Who a dashboard is cut for; each audience has its own metric set (FR-RPT-001).</summary>
public enum DashboardAudience
{
    Executive = 0,
    Scheme = 1,
    Source = 2,
    Operations = 3,
    Remediation = 4,
    Testing = 5,
    Cutover = 6
}

/// <summary>Which way a metric should move, so a UI can colour it without hard-coding metric names.</summary>
public enum MetricDirection
{
    Neutral = 0,
    HigherIsBetter = 1,
    LowerIsBetter = 2
}

public enum MetricUnit
{
    Count = 0,
    Percent = 1,
    Text = 2
}

/// <summary>
/// Whether every upstream contributing to a snapshot answered. A partially sourced dashboard is still
/// shown, but it is labelled so nobody quotes it as evidence (FR-RPT-002).
/// </summary>
public enum ReconciliationStatus
{
    Reconciled = 0,
    Partial = 1,
    Unreconciled = 2
}
