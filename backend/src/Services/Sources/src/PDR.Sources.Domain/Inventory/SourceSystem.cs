using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;

namespace PDR.Sources.Domain.Inventory;

/// <summary>
/// A registered system that originates or carries payment party addresses (FR-SRC-001). It owns its
/// field mappings (FR-SRC-003), its lineage to the submitted message (FR-SRC-002), and the operational
/// state the programme steers on: scan coverage, data freshness, mapping readiness and owner
/// attestation (FR-SRC-005, FR-SRC-006).
/// </summary>
public sealed class SourceSystem : AggregateRoot
{
    private readonly List<FieldMapping> _mappings = [];
    private readonly List<LineageStep> _lineage = [];

    private SourceSystem()
    {
    }

    private SourceSystem(
        string code,
        string name,
        SourceKind kind,
        InterfaceKind interfaceKind,
        string ownerName,
        string ownerEmail,
        string legalEntity,
        string schemeCodes,
        string? schedule,
        long estimatedPartyCount,
        long recurringInstructionCount,
        bool isAuthoritative)
    {
        Code = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(code), 32).ToUpperInvariant();
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 128);
        Kind = kind;
        Interface = interfaceKind;
        OwnerName = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(ownerName), 128);
        OwnerEmail = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(ownerEmail), 256);
        LegalEntity = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(legalEntity), 64);
        SchemeCodes = schemeCodes.ToUpperInvariant();
        Schedule = schedule;
        EstimatedPartyCount = estimatedPartyCount;
        RecurringInstructionCount = recurringInstructionCount;
        IsAuthoritative = isAuthoritative;
        Status = OnboardingStatus.Registered;
        Mapping = MappingReadiness.NotStarted;
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public SourceKind Kind { get; private set; }

    public InterfaceKind Interface { get; private set; }

    public string OwnerName { get; private set; } = string.Empty;

    public string OwnerEmail { get; private set; } = string.Empty;

    public string LegalEntity { get; private set; } = string.Empty;

    /// <summary>Comma-separated scheme codes this source feeds, e.g. <c>SEPA,CBPR</c>.</summary>
    public string SchemeCodes { get; private set; } = string.Empty;

    public string? Schedule { get; private set; }

    /// <summary>Population size used to weight remediation effort (FR-SRC-001).</summary>
    public long EstimatedPartyCount { get; private set; }

    /// <summary>Standing orders, mandates and templates that will generate future payments (FR-SRC-004).</summary>
    public long RecurringInstructionCount { get; private set; }

    public bool IsAuthoritative { get; private set; }

    public OnboardingStatus Status { get; private set; }

    public MappingReadiness Mapping { get; private set; }

    /// <summary>Share of the source population already scanned, 0-100 (FR-SRC-005).</summary>
    public decimal ScanCoveragePercent { get; private set; }

    public DateTimeOffset? LastScanAtUtc { get; private set; }

    public DateTimeOffset? LastAttestedAtUtc { get; private set; }

    public string? LastAttestedBy { get; private set; }

    public string? RemediationOwner { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<FieldMapping> Mappings => _mappings.AsReadOnly();

    public IReadOnlyCollection<LineageStep> Lineage => _lineage.OrderBy(step => step.Sequence).ToList().AsReadOnly();

    public static SourceSystem Register(
        string code,
        string name,
        SourceKind kind,
        InterfaceKind interfaceKind,
        string ownerName,
        string ownerEmail,
        string legalEntity,
        string schemeCodes,
        string? schedule,
        long estimatedPartyCount,
        long recurringInstructionCount,
        bool isAuthoritative) =>
        new(
            code,
            name,
            kind,
            interfaceKind,
            ownerName,
            ownerEmail,
            legalEntity,
            schemeCodes,
            schedule,
            estimatedPartyCount,
            recurringInstructionCount,
            isAuthoritative);

    public void Update(
        string name,
        SourceKind kind,
        InterfaceKind interfaceKind,
        string ownerName,
        string ownerEmail,
        string legalEntity,
        string schemeCodes,
        string? schedule,
        long estimatedPartyCount,
        long recurringInstructionCount,
        bool isAuthoritative,
        OnboardingStatus status,
        MappingReadiness mapping,
        string? remediationOwner,
        bool isActive)
    {
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 128);
        Kind = kind;
        Interface = interfaceKind;
        OwnerName = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(ownerName), 128);
        OwnerEmail = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(ownerEmail), 256);
        LegalEntity = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(legalEntity), 64);
        SchemeCodes = schemeCodes.ToUpperInvariant();
        Schedule = schedule;
        EstimatedPartyCount = estimatedPartyCount;
        RecurringInstructionCount = recurringInstructionCount;
        IsAuthoritative = isAuthoritative;
        Status = status;
        Mapping = mapping;
        RemediationOwner = remediationOwner;
        IsActive = isActive;
    }

    public Result AddMapping(
        string sourceAttribute,
        string targetElement,
        string? transformation,
        bool isAuthoritative,
        string? notes)
    {
        var duplicate = _mappings.Any(mapping =>
            string.Equals(mapping.SourceAttribute, sourceAttribute, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(mapping.TargetElement, targetElement, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            return Result.Failure(SourceErrors.DuplicateMapping);
        }

        _mappings.Add(FieldMapping.Create(sourceAttribute, targetElement, transformation, isAuthoritative, notes));

        if (Mapping == MappingReadiness.NotStarted)
        {
            Mapping = MappingReadiness.InProgress;
        }

        return Result.Success();
    }

    public Result RemoveMapping(Guid mappingId)
    {
        var mapping = _mappings.FirstOrDefault(entry => entry.Id == mappingId);
        if (mapping is null)
        {
            return Result.Failure(SourceErrors.MappingNotFound(mappingId));
        }

        _mappings.Remove(mapping);
        return Result.Success();
    }

    /// <summary>Replaces the lineage path; hops are renumbered so the stored sequence is always contiguous.</summary>
    public void ReplaceLineage(IEnumerable<(string FromNode, string ToNode, string? Channel, string? Description)> steps)
    {
        _lineage.Clear();

        var sequence = 1;
        foreach (var step in steps)
        {
            _lineage.Add(LineageStep.Create(sequence, step.FromNode, step.ToNode, step.Channel, step.Description));
            sequence++;
        }
    }

    public Result RecordScan(decimal coveragePercent, DateTimeOffset scannedAtUtc)
    {
        if (coveragePercent is < 0 or > 100)
        {
            return Result.Failure(SourceErrors.InvalidScanCoverage());
        }

        ScanCoveragePercent = coveragePercent;
        LastScanAtUtc = scannedAtUtc;

        if (Status is OnboardingStatus.Registered or OnboardingStatus.Onboarding)
        {
            Status = OnboardingStatus.Scanning;
        }

        return Result.Success();
    }

    /// <summary>Owner attestation that the inventory and mappings are still correct (FR-SRC-006).</summary>
    public void Attest(string attestedBy, DateTimeOffset attestedAtUtc)
    {
        LastAttestedBy = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(attestedBy), 128);
        LastAttestedAtUtc = attestedAtUtc;

        foreach (var mapping in _mappings)
        {
            mapping.MarkReviewed(attestedAtUtc);
        }
    }

    /// <summary>An attestation older than the configured interval escalates as stale (FR-SRC-006).</summary>
    public bool IsAttestationOverdue(DateTimeOffset nowUtc, int intervalDays) =>
        IsActive &&
        (LastAttestedAtUtc is null || LastAttestedAtUtc.Value.AddDays(intervalDays) < nowUtc);

    /// <summary>
    /// Composite onboarding readiness of the source, 0-100: half scan coverage, a quarter mapping
    /// maturity, a quarter attestation freshness. It is what the programme dashboard ranks sources by.
    /// </summary>
    public decimal ReadinessScore(DateTimeOffset nowUtc, int attestationIntervalDays, int freshnessDays)
    {
        var mappingScore = Mapping switch
        {
            MappingReadiness.Ready => 25m,
            MappingReadiness.InProgress => 12m,
            MappingReadiness.NeedsRework => 5m,
            _ => 0m
        };

        var attestationScore = IsAttestationOverdue(nowUtc, attestationIntervalDays) ? 0m : 25m;

        var freshScan = LastScanAtUtc is not null && LastScanAtUtc.Value.AddDays(freshnessDays) >= nowUtc;
        var coverageScore = freshScan ? ScanCoveragePercent / 2m : ScanCoveragePercent / 4m;

        return Math.Round(Math.Min(100m, mappingScore + attestationScore + coverageScore), 2);
    }

    public static Error? ValidateSchemeCodes(string schemeCodes) =>
        string.IsNullOrWhiteSpace(schemeCodes)
            ? Error.Validation("SOURCE.SCHEMES_REQUIRED", "At least one scheme code must be supplied.")
            : null;
}
