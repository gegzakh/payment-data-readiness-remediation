using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Application.Cases;

/// <summary>A case as the queue lists it — enough to triage without opening it.</summary>
public sealed record CaseListItemDto(
    Guid Id,
    string CaseKey,
    string SourceCode,
    string? PartyName,
    PartyRole PartyRole,
    string? Country,
    string IssueRuleCodes,
    string AffectedSchemes,
    int Occurrences,
    int FutureExposure,
    CasePriority Priority,
    int PriorityScore,
    CaseStatus Status,
    string? Queue,
    string? AssignedTo,
    DateOnly? DueDate,
    bool IsOverdue,
    decimal? Confidence,
    Guid? CampaignId,
    DateTimeOffset OpenedAtUtc);

/// <summary>The full case: original values, proposal, evidence, decisions and history (FR-WF-005).</summary>
public sealed record CaseDetailDto(
    Guid Id,
    string CaseKey,
    string SourceCode,
    string? OwnerName,
    string? OwnerEmail,
    string? PartyName,
    PartyRole PartyRole,
    OriginalAddressDto Original,
    ProposalDto? Proposal,
    string IssueRuleCodes,
    string AffectedSchemes,
    string EvidencePointer,
    int Occurrences,
    int FutureExposure,
    CasePriority Priority,
    int PriorityScore,
    CaseStatus Status,
    string? Queue,
    string? AssignedTo,
    DateOnly? DueDate,
    bool IsOverdue,
    Guid? CampaignId,
    string? SubmittedBy,
    DateTimeOffset? SubmittedAtUtc,
    string? DecidedBy,
    DateTimeOffset? DecidedAtUtc,
    string? DecisionRationale,
    DateOnly? ExceptionExpiresOn,
    bool IsExceptionExpired,
    string? FailureReason,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? RemediatedAtUtc,
    IReadOnlyList<CaseEvidenceDto> Evidence,
    IReadOnlyList<CaseEventDto> History);

public sealed record OriginalAddressDto(
    string? Country,
    string? TownName,
    string? PostCode,
    string? StreetName,
    string? BuildingNumber,
    string? AddressLines);

public sealed record ProposalDto(
    Guid Id,
    ProposalMethod Method,
    bool RequiresHumanVerification,
    string? Country,
    string? TownName,
    string? PostCode,
    string? StreetName,
    string? BuildingNumber,
    decimal CountryConfidence,
    decimal TownConfidence,
    decimal PostCodeConfidence,
    decimal StreetConfidence,
    decimal BuildingNumberConfidence,
    decimal OverallConfidence,
    string? Ambiguity,
    string? Alternatives,
    string? Notes);

public sealed record CaseEvidenceDto(
    Guid Id,
    string Kind,
    string Reference,
    string? Description,
    string CapturedBy,
    DateTimeOffset CapturedAtUtc);

public sealed record CaseEventDto(
    Guid Id,
    string Action,
    CaseStatus FromStatus,
    CaseStatus ToStatus,
    string Actor,
    string? Rationale,
    DateTimeOffset OccurredAtUtc);

/// <summary>What generating cases from a validation run produced.</summary>
public sealed record CaseGenerationDto(
    Guid RunId,
    int AssessmentsRead,
    int CasesCreated,
    int CasesUpdated,
    int OccurrencesFolded,
    DateTimeOffset GeneratedAtUtc);

/// <summary>The remediation funnel: how much exposure sits in each stage (FR-REP-001).</summary>
public sealed record RemediationFunnelDto(
    int TotalCases,
    int OpenCases,
    int PendingApproval,
    int Approved,
    int Remediated,
    int Dismissed,
    int Rejected,
    int ExceptionsGranted,
    int ExpiredExceptions,
    int Overdue,
    int FutureExposureOpen,
    int FutureExposureRemediated,
    decimal RemediationPercent,
    IReadOnlyList<FunnelBucketDto> ByPriority,
    IReadOnlyList<FunnelBucketDto> BySource,
    DateTimeOffset AsOfUtc);

public sealed record FunnelBucketDto(string Key, int CaseCount, int OpenCount, int FutureExposure);

/// <summary>What a bulk action would touch, before anybody commits to it (FR-REM-007).</summary>
public sealed record BulkPreviewDto(
    string Action,
    int MatchedCases,
    int EligibleCases,
    int BlockedCases,
    int FutureExposure,
    decimal? LowestConfidence,
    bool RollbackSupported,
    IReadOnlyList<string> BlockedReasons,
    IReadOnlyList<CaseListItemDto> Sample);

public sealed record BulkResultDto(string Action, int Applied, int Skipped, IReadOnlyList<string> Failures);

public static class RemediationDefaults
{
    public const int PageSize = 20;
    public const int MaxPageSize = 200;
    public const int BulkPreviewSampleSize = 10;

    /// <summary>Below this a proposal cannot be bulk-approved; a human must look at it (FR-REM-007).</summary>
    public const decimal BulkApprovalMinimumConfidence = 90m;

    /// <summary>Corrections that add data the source never held need evidence (FR-WF-004).</summary>
    public const bool EvidenceRequiredForNewData = true;

    public const int DefaultSlaDays = 14;
    public const string DefaultQueue = "data-quality";
    public const string CriticalSchemes = "SEPA,TARGET2";
    public const string CutoverDate = "2026-11-22";
}

public static class RemediationSettingKeys
{
    public const string PageSize = "Remediation:Paging:DefaultPageSize";
    public const string BulkApprovalMinimumConfidence = "Remediation:Bulk:MinimumConfidence";
    public const string EvidenceRequiredForNewData = "Remediation:Workflow:EvidenceRequiredForNewData";
    public const string SlaDays = "Remediation:Workflow:SlaDays";
    public const string DefaultQueue = "Remediation:Workflow:DefaultQueue";
    public const string CriticalSchemes = "Remediation:Prioritization:CriticalSchemes";
    public const string CutoverDate = "Remediation:Prioritization:CutoverDate";
}
