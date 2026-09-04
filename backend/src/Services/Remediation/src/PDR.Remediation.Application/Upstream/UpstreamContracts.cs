using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Application.Upstream;

/// <summary>A validation run as remediation needs it: enough to know what was assessed and when.</summary>
public sealed record ValidationRunSummary(
    Guid Id,
    Guid BatchId,
    string SourceCode,
    string SchemeCode,
    string Status,
    DateOnly AsOf,
    int AssessedCount,
    int PaymentsAtRisk);

/// <summary>
/// One assessed address with its findings, read unmasked from the validation service so a case can carry
/// the original value the maker has to correct (FR-REM-002).
/// </summary>
public sealed record AssessedAddress(
    Guid Id,
    string SourceCode,
    string? SchemeCode,
    string? MessageId,
    string? EndToEndId,
    PartyRole PartyRole,
    string? PartyName,
    string Classification,
    string CurrentOutcome,
    string FutureOutcome,
    string? Country,
    string? TownName,
    string? PostCode,
    string? StreetName,
    string? BuildingNumber,
    string? AddressLines,
    string EvidencePointer,
    IReadOnlyList<AssessedIssue> Issues);

public sealed record AssessedIssue(string Mode, string RuleCode, string Field, string Severity, string Message);

/// <summary>Reads validation output; the validation service stays the owner of the assessment (FR-REM-001).</summary>
public interface IValidationGateway
{
    Task<ValidationRunSummary?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default);

    Task<ValidationRunSummary?> GetLatestRunAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssessedAddress>> GetAssessmentsAsync(Guid runId, CancellationToken cancellationToken = default);
}

/// <summary>The owning source and its steward, so a case can be routed to whoever can fix it (FR-SRC-002).</summary>
public sealed record SourceOwner(string SourceCode, string? OwnerName, string? OwnerEmail);

public interface ISourcesGateway
{
    Task<SourceOwner?> GetOwnerAsync(string sourceCode, CancellationToken cancellationToken = default);
}

/// <summary>Raised when an upstream service is reachable but answers with an error.</summary>
public sealed class UpstreamException(string service, string message) : Exception(message)
{
    public string Service { get; } = service;
}
