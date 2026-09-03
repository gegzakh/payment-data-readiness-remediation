using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;

namespace PDR.Validation.Domain.Assessments;

/// <summary>
/// One evaluation of an ingested batch against a scheme's current and future rule sets. It keeps the
/// rule versions it used so a readiness number can always be explained and reproduced (FR-VAL-004).
/// </summary>
public sealed class ValidationRun : AggregateRoot
{
    private ValidationRun()
    {
    }

    private ValidationRun(
        Guid batchId,
        string sourceCode,
        string schemeCode,
        DateOnly asOf,
        DateTimeOffset startedAtUtc)
    {
        BatchId = batchId;
        SourceCode = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(sourceCode), 32).ToUpperInvariant();
        SchemeCode = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(schemeCode), 32).ToUpperInvariant();
        AsOf = asOf;
        StartedAtUtc = startedAtUtc;
        Status = ValidationRunStatus.Running;
    }

    public Guid BatchId { get; private set; }

    public string SourceCode { get; private set; } = string.Empty;

    public string SchemeCode { get; private set; } = string.Empty;

    public DateOnly AsOf { get; private set; }

    public int? CurrentRulesetVersion { get; private set; }

    public int? FutureRulesetVersion { get; private set; }

    public ValidationRunStatus Status { get; private set; }

    public string? ErrorSummary { get; private set; }

    public int InputRecordCount { get; private set; }

    public int AssessedCount { get; private set; }

    public int ExcludedCount { get; private set; }

    public int UnableToAssessCount { get; private set; }

    public int CurrentCompliantCount { get; private set; }

    public int CurrentRejectedCount { get; private set; }

    public int CurrentWarningCount { get; private set; }

    public int FutureCompliantCount { get; private set; }

    public int FutureRejectedCount { get; private set; }

    public int FutureWarningCount { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <summary>Share of assessed records compliant today (FR-VAL-010).</summary>
    public decimal CurrentReadinessPercent => Percent(CurrentCompliantCount);

    /// <summary>Share of assessed records that will still be compliant after the cutover (FR-VAL-010).</summary>
    public decimal FutureReadinessPercent => Percent(FutureCompliantCount);

    /// <summary>Records the scheme would reject once the future rules apply — the payments at risk.</summary>
    public int PaymentsAtRisk => FutureRejectedCount;

    public static ValidationRun Start(
        Guid batchId,
        string sourceCode,
        string schemeCode,
        DateOnly asOf,
        DateTimeOffset startedAtUtc) =>
        new(batchId, sourceCode, schemeCode, asOf, startedAtUtc);

    public void RecordRulesetVersions(int? currentVersion, int? futureVersion)
    {
        CurrentRulesetVersion = currentVersion;
        FutureRulesetVersion = futureVersion;
    }

    public void Complete(IReadOnlyCollection<AddressAssessment> assessments, DateTimeOffset completedAtUtc)
    {
        InputRecordCount = assessments.Count;
        ExcludedCount = assessments.Count(assessment => assessment.CurrentOutcome == RecordOutcome.Excluded);
        UnableToAssessCount = assessments.Count(assessment => assessment.CurrentOutcome == RecordOutcome.UnableToAssess);
        AssessedCount = InputRecordCount - ExcludedCount - UnableToAssessCount;

        CurrentCompliantCount = Count(assessments, RuleMode.Current, RecordOutcome.Compliant);
        CurrentRejectedCount = Count(assessments, RuleMode.Current, RecordOutcome.Rejected);
        CurrentWarningCount = Count(assessments, RuleMode.Current, RecordOutcome.Warning);
        FutureCompliantCount = Count(assessments, RuleMode.Future, RecordOutcome.Compliant);
        FutureRejectedCount = Count(assessments, RuleMode.Future, RecordOutcome.Rejected);
        FutureWarningCount = Count(assessments, RuleMode.Future, RecordOutcome.Warning);

        Status = ValidationRunStatus.Completed;
        CompletedAtUtc = completedAtUtc;
    }

    public void Fail(string errorSummary, DateTimeOffset completedAtUtc)
    {
        Status = ValidationRunStatus.Failed;
        ErrorSummary = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(errorSummary), 1024);
        CompletedAtUtc = completedAtUtc;
    }

    /// <summary>Every input record must land in exactly one bucket of each mode (FR-VAL-008).</summary>
    public bool CountsReconcile()
    {
        if (Status is not ValidationRunStatus.Completed)
        {
            return true;
        }

        var informational = InputRecordCount
            - ExcludedCount
            - UnableToAssessCount
            - CurrentCompliantCount
            - CurrentRejectedCount
            - CurrentWarningCount;

        return informational >= 0 && AssessedCount == InputRecordCount - ExcludedCount - UnableToAssessCount;
    }

    private static int Count(
        IReadOnlyCollection<AddressAssessment> assessments,
        RuleMode mode,
        RecordOutcome outcome) =>
        assessments.Count(assessment =>
            (mode == RuleMode.Current ? assessment.CurrentOutcome : assessment.FutureOutcome) == outcome);

    private decimal Percent(int compliant) =>
        AssessedCount == 0 ? 0m : Math.Round(compliant * 100m / AssessedCount, 2);
}

public static class ValidationErrors
{
    public static Error RunNotFound(Guid id) =>
        Error.NotFound("VALIDATION.RUN_NOT_FOUND", $"Validation run '{id}' was not found.");

    public static Error BatchNotFound(Guid id) =>
        Error.NotFound("VALIDATION.BATCH_NOT_FOUND", $"Ingestion batch '{id}' was not found.");

    public static Error BatchNotParsed(string status) =>
        Error.Conflict(
            "VALIDATION.BATCH_NOT_PARSED",
            $"Only a parsed batch can be validated; this one is '{status}'.");

    public static readonly Error NoRecords =
        Error.Conflict("VALIDATION.NO_RECORDS", "The batch contains no party address records to validate.");

    public static Error UpstreamUnavailable(string service) =>
        Error.Dependency("VALIDATION.UPSTREAM_UNAVAILABLE", $"The {service} service could not be reached.");
}
