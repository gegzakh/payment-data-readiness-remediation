using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Application.Assess;

public sealed record ValidationRunDto(
    Guid Id,
    Guid BatchId,
    string SourceCode,
    string SchemeCode,
    DateOnly AsOf,
    int? CurrentRulesetVersion,
    int? FutureRulesetVersion,
    ValidationRunStatus Status,
    string? ErrorSummary,
    int InputRecordCount,
    int AssessedCount,
    int ExcludedCount,
    int UnableToAssessCount,
    int CurrentCompliantCount,
    int CurrentWarningCount,
    int CurrentRejectedCount,
    int FutureCompliantCount,
    int FutureWarningCount,
    int FutureRejectedCount,
    decimal CurrentReadinessPercent,
    decimal FutureReadinessPercent,
    int PaymentsAtRisk,
    bool CountsReconcile,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record ValidationIssueDto(
    Guid Id,
    RuleMode Mode,
    string RuleCode,
    string Field,
    IssueSeverity Severity,
    string Message,
    string? Expected,
    string? Actual);

public sealed record AddressAssessmentDto(
    Guid Id,
    Guid RunId,
    Guid RecordId,
    Guid BatchId,
    string SourceCode,
    int Sequence,
    string? MessageId,
    string? EndToEndId,
    PartyRole PartyRole,
    string? PartyName,
    string? Country,
    string? TownName,
    string? PostCode,
    string? StreetName,
    string? BuildingNumber,
    string? AddressLines,
    string? SchemeCode,
    bool IsDuplicate,
    AddressClassification Classification,
    RecordOutcome CurrentOutcome,
    RecordOutcome FutureOutcome,
    string EvidencePointer,
    IReadOnlyList<ValidationIssueDto> Issues);

/// <summary>One row of a profile: the dimension value and how its records break down (FR-VAL-006).</summary>
public sealed record ProfileRowDto(
    string Key,
    int RecordCount,
    int CurrentRejectedCount,
    int FutureRejectedCount,
    decimal CurrentReadinessPercent,
    decimal FutureReadinessPercent);

public sealed record ProfileDto(ProfileDimension Dimension, IReadOnlyList<ProfileRowDto> Rows, DateTimeOffset AsOfUtc);

/// <summary>Portfolio readiness across the latest run of every validated batch (FR-VAL-010).</summary>
public sealed record ReadinessSummaryDto(
    int RunCount,
    int AssessedCount,
    int ExcludedCount,
    int UnableToAssessCount,
    int CurrentRejectedCount,
    int FutureRejectedCount,
    decimal CurrentReadinessPercent,
    decimal FutureReadinessPercent,
    int PaymentsAtRisk,
    IReadOnlyList<IssueSummaryDto> TopIssues,
    DateTimeOffset AsOfUtc);

public sealed record IssueSummaryDto(string RuleCode, string Field, IssueSeverity Severity, RuleMode Mode, int Count);

public static class ValidationSettingKeys
{
    public const string PageSize = "validation.page-size";
    public const string DefaultSchemeCode = "validation.default-scheme-code";
    public const string TopIssueCount = "validation.top-issue-count";
    public const string FutureAsOfDate = "validation.future-as-of-date";
}
