namespace PDR.Simulation.Application.Upstream;

/// <summary>
/// The portfolio as validation currently sees it. The counts are carried through into a run unchanged so a
/// stored result reconciles against the validation service that produced it (FR-SIM-001).
/// </summary>
public sealed record PortfolioSnapshot(
    int AssessedCount,
    int ExcludedCount,
    int UnableToAssessCount,
    int CurrentRejectedCount,
    int FutureRejectedCount,
    int PaymentsAtRisk,
    string? RulesetVersion,
    DateTimeOffset AsOfUtc);

/// <summary>One dimension row of the portfolio, used to store a run's breakdown.</summary>
public sealed record PortfolioProfileRow(
    string Dimension,
    string Key,
    int RecordCount,
    int CurrentRejectedCount,
    int FutureRejectedCount,
    int CurrentWarningCount,
    int FutureWarningCount);

/// <summary>What remediation has already fixed, which is what makes a remediated scenario differ from a future one.</summary>
public sealed record RemediationSnapshot(
    int TotalCases,
    int RemediatedCases,
    int ApprovedCases,
    int OpenCases,
    int ExpiredExceptions,
    int FutureExposureOpen,
    int FutureExposureRemediated);

public interface IPortfolioGateway
{
    Task<PortfolioSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PortfolioProfileRow>> GetProfileAsync(string dimension, CancellationToken cancellationToken = default);
}

public interface IRemediationGateway
{
    Task<RemediationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public sealed class UpstreamException(string service, string message)
    : Exception($"Upstream '{service}' failed: {message}")
{
    public string Service { get; } = service;
}
