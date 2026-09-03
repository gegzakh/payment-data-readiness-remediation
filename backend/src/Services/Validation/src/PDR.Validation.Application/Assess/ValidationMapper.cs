using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Application.Assess;

public static class ValidationMapper
{
    public static ValidationRunDto ToDto(this ValidationRun run) =>
        new(
            run.Id,
            run.BatchId,
            run.SourceCode,
            run.SchemeCode,
            run.AsOf,
            run.CurrentRulesetVersion,
            run.FutureRulesetVersion,
            run.Status,
            run.ErrorSummary,
            run.InputRecordCount,
            run.AssessedCount,
            run.ExcludedCount,
            run.UnableToAssessCount,
            run.CurrentCompliantCount,
            run.CurrentWarningCount,
            run.CurrentRejectedCount,
            run.FutureCompliantCount,
            run.FutureWarningCount,
            run.FutureRejectedCount,
            run.CurrentReadinessPercent,
            run.FutureReadinessPercent,
            run.PaymentsAtRisk,
            run.CountsReconcile(),
            run.StartedAtUtc,
            run.CompletedAtUtc);

    public static ValidationIssueDto ToDto(this ValidationIssue issue) =>
        new(issue.Id, issue.Mode, issue.RuleCode, issue.Field, issue.Severity, issue.Message, issue.Expected, issue.Actual);

    /// <summary>
    /// The assessed address is personal data. Callers without the drill-down permission see the structure
    /// and the findings, but only masked values (FR-VAL-009).
    /// </summary>
    public static AddressAssessmentDto ToDto(this AddressAssessment assessment, bool unmasked) =>
        new(
            assessment.Id,
            assessment.RunId,
            assessment.RecordId,
            assessment.BatchId,
            assessment.SourceCode,
            assessment.Sequence,
            assessment.MessageId,
            unmasked ? assessment.EndToEndId : Mask(assessment.EndToEndId),
            assessment.PartyRole,
            unmasked ? assessment.PartyName : Mask(assessment.PartyName),
            assessment.Country,
            unmasked ? assessment.TownName : Mask(assessment.TownName),
            unmasked ? assessment.PostCode : Mask(assessment.PostCode),
            unmasked ? assessment.StreetName : Mask(assessment.StreetName),
            unmasked ? assessment.BuildingNumber : Mask(assessment.BuildingNumber),
            unmasked ? assessment.AddressLines : Mask(assessment.AddressLines),
            assessment.SchemeCode,
            assessment.IsDuplicate,
            assessment.Classification,
            assessment.CurrentOutcome,
            assessment.FutureOutcome,
            assessment.EvidencePointer,
            [.. assessment.Issues.OrderBy(issue => issue.Mode).ThenBy(issue => issue.RuleCode, StringComparer.Ordinal)
                .Select(ToDto)]);

    private static string? Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 2
            ? new string('*', trimmed.Length)
            : string.Concat(trimmed.AsSpan(0, 2), new string('*', Math.Min(trimmed.Length - 2, 8)));
    }
}
