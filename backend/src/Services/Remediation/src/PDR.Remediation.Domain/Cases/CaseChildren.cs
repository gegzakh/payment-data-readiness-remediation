using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;

namespace PDR.Remediation.Domain.Cases;

/// <summary>
/// The structured address a maker proposes, with the method, evidence and per-field confidence that
/// justify it (FR-REM-003, FR-REM-004). Machine assistance is labelled and never self-approves.
/// </summary>
public sealed class Proposal : Entity
{
    private Proposal()
    {
    }

    private Proposal(Guid caseId, ProposalMethod method, ProposedAddress address, string? notes)
    {
        CaseId = caseId;
        Method = method;
        Apply(address, notes);
    }

    public Guid CaseId { get; private set; }

    public ProposalMethod Method { get; private set; }

    public string? Country { get; private set; }

    public string? TownName { get; private set; }

    public string? PostCode { get; private set; }

    public string? StreetName { get; private set; }

    public string? BuildingNumber { get; private set; }

    public decimal CountryConfidence { get; private set; }

    public decimal TownConfidence { get; private set; }

    public decimal PostCodeConfidence { get; private set; }

    public decimal StreetConfidence { get; private set; }

    public decimal BuildingNumberConfidence { get; private set; }

    /// <summary>Weakest field carries the proposal: a correction is only as good as its worst part.</summary>
    public decimal OverallConfidence { get; private set; }

    /// <summary>What the parser could not resolve, so a reviewer knows where to look (FR-REM-004).</summary>
    public string? Ambiguity { get; private set; }

    /// <summary>Other readings of the same input, kept so the reviewer can pick one.</summary>
    public string? Alternatives { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>Machine assistance must be verified by a human before it can be approved (FR-REM-005).</summary>
    public bool RequiresHumanVerification => Method == ProposalMethod.AssistedSuggestion;

    public static Proposal Create(Guid caseId, ProposalMethod method, ProposedAddress address, string? notes) =>
        new(caseId, method, address, notes);

    public void Revise(ProposalMethod method, ProposedAddress address, string? notes)
    {
        Method = method;
        Apply(address, notes);
    }

    private void Apply(ProposedAddress address, string? notes)
    {
        Country = Trim(address.Country, 8)?.ToUpperInvariant();
        TownName = Trim(address.TownName, 140);
        PostCode = Trim(address.PostCode, 32);
        StreetName = Trim(address.StreetName, 140);
        BuildingNumber = Trim(address.BuildingNumber, 32);

        CountryConfidence = Clamp(address.Confidence.Country);
        TownConfidence = Clamp(address.Confidence.Town);
        PostCodeConfidence = Clamp(address.Confidence.PostCode);
        StreetConfidence = Clamp(address.Confidence.Street);
        BuildingNumberConfidence = Clamp(address.Confidence.BuildingNumber);

        OverallConfidence = Math.Min(
            Math.Min(CountryConfidence, TownConfidence),
            Math.Min(PostCodeConfidence, Math.Min(StreetConfidence, BuildingNumberConfidence)));

        Ambiguity = Trim(address.Ambiguity, 512);
        Alternatives = Trim(address.Alternatives, 1024);
        Notes = Trim(notes, 1024);
    }

    private static string? Trim(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];

    private static decimal Clamp(decimal value) => Math.Round(Math.Clamp(value, 0m, 100m), 2);
}

/// <summary>A proposed structured address with the confidence and caveats behind it.</summary>
public sealed record ProposedAddress(
    string? Country,
    string? TownName,
    string? PostCode,
    string? StreetName,
    string? BuildingNumber,
    FieldConfidence Confidence,
    string? Ambiguity = null,
    string? Alternatives = null);

/// <summary>Confidence per field, 0-100 (FR-REM-004).</summary>
public sealed record FieldConfidence(
    decimal Country,
    decimal Town,
    decimal PostCode,
    decimal Street,
    decimal BuildingNumber)
{
    public static readonly FieldConfidence Certain = new(100m, 100m, 100m, 100m, 100m);
}

/// <summary>Anything a maker attaches to support a correction (FR-WF-002, FR-WF-004).</summary>
public sealed class CaseEvidence : Entity
{
    private CaseEvidence()
    {
    }

    private CaseEvidence(Guid caseId, string kind, string reference, string? description, string capturedBy, DateTimeOffset capturedAtUtc)
    {
        CaseId = caseId;
        Kind = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(kind), 64);
        Reference = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(reference), 512);
        Description = description is null ? null : Ensure.MaxLength(description, 1024);
        CapturedBy = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(capturedBy), 128);
        CapturedAtUtc = capturedAtUtc;
    }

    public Guid CaseId { get; private set; }

    public string Kind { get; private set; } = string.Empty;

    public string Reference { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string CapturedBy { get; private set; } = string.Empty;

    public DateTimeOffset CapturedAtUtc { get; private set; }

    public static CaseEvidence Create(
        Guid caseId,
        string kind,
        string reference,
        string? description,
        string capturedBy,
        DateTimeOffset capturedAtUtc) =>
        new(caseId, kind, reference, description, capturedBy, capturedAtUtc);
}

/// <summary>An immutable line in the case history: who did what, when and why (FR-WF-005, FR-AUD-002).</summary>
public sealed class CaseEvent : Entity
{
    private CaseEvent()
    {
    }

    private CaseEvent(
        Guid caseId,
        string action,
        CaseStatus fromStatus,
        CaseStatus toStatus,
        string actor,
        string? rationale,
        DateTimeOffset occurredAtUtc)
    {
        CaseId = caseId;
        Action = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(action), 64);
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Actor = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(actor), 128);
        Rationale = rationale is null ? null : Ensure.MaxLength(rationale, 1024);
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid CaseId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public CaseStatus FromStatus { get; private set; }

    public CaseStatus ToStatus { get; private set; }

    public string Actor { get; private set; } = string.Empty;

    public string? Rationale { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static CaseEvent Create(
        Guid caseId,
        string action,
        CaseStatus fromStatus,
        CaseStatus toStatus,
        string actor,
        string? rationale,
        DateTimeOffset occurredAtUtc) =>
        new(caseId, action, fromStatus, toStatus, actor, rationale, occurredAtUtc);
}
