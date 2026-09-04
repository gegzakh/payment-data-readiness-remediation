namespace PDR.Reporting.Application.Upstream;

/// <summary>Portfolio readiness as validation currently sees it (FR-VAL-008).</summary>
public sealed record ValidationSnapshot(
    int AssessedCount,
    int ExcludedCount,
    int UnableToAssessCount,
    int CurrentRejectedCount,
    int FutureRejectedCount,
    int CurrentWarningCount,
    int FutureWarningCount,
    int PaymentsAtRisk,
    string? RulesetVersion,
    DateTimeOffset AsOfUtc)
{
    public static readonly ValidationSnapshot Empty =
        new(0, 0, 0, 0, 0, 0, 0, 0, null, DateTimeOffset.UnixEpoch);

    public int PopulationCount => AssessedCount + ExcludedCount + UnableToAssessCount;

    public decimal FutureReadinessPercent =>
        AssessedCount == 0 ? 0m : Math.Round((AssessedCount - FutureRejectedCount) * 100m / AssessedCount, 2);

    public decimal CurrentReadinessPercent =>
        AssessedCount == 0 ? 0m : Math.Round((AssessedCount - CurrentRejectedCount) * 100m / AssessedCount, 2);
}

public sealed record ValidationProfileRow(
    string Dimension,
    string Key,
    int RecordCount,
    int CurrentRejectedCount,
    int FutureRejectedCount,
    int CurrentWarningCount,
    int FutureWarningCount);

/// <summary>Where remediation has got to (FR-WF-007).</summary>
public sealed record RemediationSnapshot(
    int TotalCases,
    int OpenCases,
    int ApprovedCases,
    int RemediatedCases,
    int ExpiredExceptions,
    int FutureExposureOpen,
    int FutureExposureRemediated)
{
    public static readonly RemediationSnapshot Empty = new(0, 0, 0, 0, 0, 0, 0);

    public decimal CompletionPercent =>
        TotalCases == 0 ? 0m : Math.Round(RemediatedCases * 100m / TotalCases, 2);
}

/// <summary>The last stored simulation run and the current cutover position (FR-SIM-002, FR-CUT-002).</summary>
public sealed record SimulationSnapshot(
    Guid? LatestRunId,
    string? LatestRunScenario,
    DateTimeOffset? LatestRunAtUtc,
    int RemediatedRejectedCount,
    int RemediatedPaymentsAtRisk,
    decimal RemediatedReadinessPercent,
    string? Recommendation,
    int ResidualExposure,
    int EntryCriteriaOutstanding,
    int ExitCriteriaOutstanding,
    int WaivedCriteria,
    int OpenDefects,
    int UatMismatches,
    decimal TestCoveragePercent,
    string? RulesetVersion)
{
    public static readonly SimulationSnapshot Empty =
        new(null, null, null, 0, 0, 0m, null, 0, 0, 0, 0, 0, 0, 0m, null);
}

public interface IValidationGateway
{
    Task<ValidationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ValidationProfileRow>> GetProfileAsync(string dimension, CancellationToken cancellationToken = default);
}

public interface IRemediationGateway
{
    Task<RemediationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public interface ISimulationGateway
{
    Task<SimulationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

/// <summary>Raised when an upstream answers with something other than data or a benign "nothing yet".</summary>
public sealed class UpstreamException(string service, string message) : Exception(message)
{
    public string Service { get; } = service;
}
