using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Application.Upstream;

/// <summary>The subset of an ingestion batch validation needs; the ingestion service owns the rest.</summary>
public sealed record IngestedBatch(Guid Id, string SourceCode, string Status, int ParsedCount);

/// <summary>One ingested party address, unmasked, as read from the ingestion service.</summary>
public sealed record IngestedRecord(
    Guid Id,
    Guid BatchId,
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
    bool IsDuplicate);

/// <summary>A rule as published by the rules service, in the shape the evaluator consumes.</summary>
public sealed record RuleSnapshot(
    string Code,
    string Field,
    RuleCheck Kind,
    IssueSeverity Severity,
    string Message,
    string? Parameter);

/// <summary>The deterministic check a rule performs; mirrors the rules service rule kinds.</summary>
public enum RuleCheck
{
    Required = 0,
    MaxLength = 1,
    Pattern = 2,
    AllowedValues = 3,
    Prohibited = 4,
    StructuredOnly = 5
}

/// <summary>The rule set in force for a scheme on a date, for one mode.</summary>
public sealed record EffectiveRuleset(
    string SchemeCode,
    int VersionNumber,
    DateOnly AsOf,
    RuleMode Mode,
    IReadOnlyList<RuleSnapshot> Rules);

/// <summary>Reads ingested batches and their party records (FR-VAL-001).</summary>
public interface IIngestionGateway
{
    Task<IngestedBatch?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IngestedRecord>> GetRecordsAsync(Guid batchId, CancellationToken cancellationToken = default);
}

/// <summary>Reads the current and post-cutover rule sets a scheme enforces (FR-VAL-004).</summary>
public interface IRulesGateway
{
    Task<EffectiveRuleset?> GetEffectiveRulesetAsync(
        string schemeCode,
        DateOnly asOf,
        RuleMode mode,
        CancellationToken cancellationToken = default);
}

/// <summary>Raised when an upstream service is reachable but answers with an error.</summary>
public sealed class UpstreamException(string service, string message) : Exception(message)
{
    public string Service { get; } = service;
}
