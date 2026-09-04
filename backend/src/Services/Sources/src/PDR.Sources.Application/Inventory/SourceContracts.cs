using PDR.Sources.Domain.Inventory;

namespace PDR.Sources.Application.Inventory;

public sealed record FieldMappingDto(
    Guid Id,
    string SourceAttribute,
    string TargetElement,
    string? Transformation,
    bool IsAuthoritative,
    string? Notes,
    DateTimeOffset? LastReviewedAtUtc);

public sealed record LineageStepDto(
    int Sequence,
    string FromNode,
    string ToNode,
    string? Channel,
    string? Description);

public sealed record SourceSystemDto(
    Guid Id,
    string Code,
    string Name,
    SourceKind Kind,
    InterfaceKind Interface,
    string OwnerName,
    string OwnerEmail,
    string LegalEntity,
    IReadOnlyList<string> SchemeCodes,
    string? Schedule,
    long EstimatedPartyCount,
    long RecurringInstructionCount,
    bool IsAuthoritative,
    OnboardingStatus Status,
    MappingReadiness Mapping,
    decimal ScanCoveragePercent,
    DateTimeOffset? LastScanAtUtc,
    DateTimeOffset? LastAttestedAtUtc,
    string? LastAttestedBy,
    bool AttestationOverdue,
    decimal ReadinessScore,
    string? RemediationOwner,
    bool IsActive,
    IReadOnlyList<FieldMappingDto> Mappings,
    IReadOnlyList<LineageStepDto> Lineage);

/// <summary>Portfolio view the programme steers on (FR-SRC-005).</summary>
public sealed record SourceReadinessSummaryDto(
    int TotalSources,
    int ReadySources,
    int BlockedSources,
    int AttestationOverdueSources,
    long CoveredPartyCount,
    long TotalPartyCount,
    decimal AverageReadinessScore,
    DateTimeOffset AsOfUtc);

public sealed record FieldMappingInput(
    string SourceAttribute,
    string TargetElement,
    string? Transformation,
    bool IsAuthoritative,
    string? Notes);

public sealed record LineageStepInput(string FromNode, string ToNode, string? Channel, string? Description);

public static class SourcesSettingKeys
{
    /// <summary>Days after which an owner attestation is stale and escalates (FR-SRC-006).</summary>
    public const string AttestationIntervalDays = "sources.attestation.interval-days";

    /// <summary>Days after which the last scan no longer counts as fresh (FR-SRC-005).</summary>
    public const string ScanFreshnessDays = "sources.scan.freshness-days";
}
