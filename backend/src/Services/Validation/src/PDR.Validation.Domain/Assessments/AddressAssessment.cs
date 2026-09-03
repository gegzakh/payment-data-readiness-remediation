using PDR.BuildingBlocks.Domain;

namespace PDR.Validation.Domain.Assessments;

/// <summary>
/// The verdict for one ingested party address under the current and the future rule set, together with
/// the address snapshot needed for drill-down and the findings that explain it (FR-VAL-002, FR-VAL-005).
/// </summary>
public sealed class AddressAssessment : Entity
{
    private readonly List<ValidationIssue> _issues = [];

    private AddressAssessment()
    {
    }

    private AddressAssessment(Guid runId, AddressSnapshot snapshot)
    {
        RunId = runId;
        RecordId = snapshot.RecordId;
        Sequence = snapshot.Sequence;
        MessageId = snapshot.MessageId;
        EndToEndId = snapshot.EndToEndId;
        PartyRole = snapshot.PartyRole;
        PartyName = snapshot.PartyName;
        Country = snapshot.Country;
        TownName = snapshot.TownName;
        PostCode = snapshot.PostCode;
        StreetName = snapshot.StreetName;
        BuildingNumber = snapshot.BuildingNumber;
        AddressLines = snapshot.AddressLines;
        SchemeCode = snapshot.SchemeCode;
        SourceCode = snapshot.SourceCode;
        BatchId = snapshot.BatchId;
        IsDuplicate = snapshot.IsDuplicate;
        Classification = snapshot.Classification;
        EvidencePointer = $"batch:{snapshot.BatchId}#record:{snapshot.Sequence}";
    }

    public Guid RunId { get; private set; }

    public Guid RecordId { get; private set; }

    public Guid BatchId { get; private set; }

    public string SourceCode { get; private set; } = string.Empty;

    public int Sequence { get; private set; }

    public string? MessageId { get; private set; }

    public string? EndToEndId { get; private set; }

    public PartyRole PartyRole { get; private set; }

    public string? PartyName { get; private set; }

    public string? Country { get; private set; }

    public string? TownName { get; private set; }

    public string? PostCode { get; private set; }

    public string? StreetName { get; private set; }

    public string? BuildingNumber { get; private set; }

    public string? AddressLines { get; private set; }

    public string? SchemeCode { get; private set; }

    public bool IsDuplicate { get; private set; }

    public AddressClassification Classification { get; private set; }

    public RecordOutcome CurrentOutcome { get; private set; }

    public RecordOutcome FutureOutcome { get; private set; }

    /// <summary>Where the assessed data came from, so a reviewer can go back to the raw batch.</summary>
    public string EvidencePointer { get; private set; } = string.Empty;

    public IReadOnlyCollection<ValidationIssue> Issues => _issues.AsReadOnly();

    public static AddressAssessment Create(Guid runId, AddressSnapshot snapshot) => new(runId, snapshot);

    public void AddIssue(
        RuleMode mode,
        string ruleCode,
        string field,
        IssueSeverity severity,
        string message,
        string? expected,
        string? actual) =>
        _issues.Add(ValidationIssue.Create(Id, mode, ruleCode, field, severity, message, expected, actual));

    /// <summary>
    /// Duplicates are excluded rather than counted twice, and a record that no rule set could be found
    /// for is recorded as unable-to-assess instead of silently passing (FR-VAL-007).
    /// </summary>
    public void Conclude(bool currentRulesAvailable, bool futureRulesAvailable)
    {
        CurrentOutcome = Decide(RuleMode.Current, currentRulesAvailable);
        FutureOutcome = Decide(RuleMode.Future, futureRulesAvailable);
    }

    private RecordOutcome Decide(RuleMode mode, bool rulesAvailable)
    {
        if (IsDuplicate)
        {
            return RecordOutcome.Excluded;
        }

        if (!rulesAvailable)
        {
            return RecordOutcome.UnableToAssess;
        }

        var findings = _issues.Where(issue => issue.Mode == mode).ToList();

        if (findings.Exists(issue => issue.Severity == IssueSeverity.Error))
        {
            return RecordOutcome.Rejected;
        }

        if (findings.Exists(issue => issue.Severity == IssueSeverity.Warning))
        {
            return RecordOutcome.Warning;
        }

        return findings.Count > 0 ? RecordOutcome.Informational : RecordOutcome.Compliant;
    }
}

/// <summary>The ingested record plus its classification, handed to the aggregate as one value.</summary>
public sealed record AddressSnapshot(
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
    AddressClassification Classification);
