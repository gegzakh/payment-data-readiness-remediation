using PDR.BuildingBlocks.Domain;

namespace PDR.Validation.Domain.Assessments;

/// <summary>
/// One rule finding against one address field: what was expected, what was actually there, and the
/// pointer back to the evidence in the ingested batch (FR-VAL-005).
/// </summary>
public sealed class ValidationIssue : Entity
{
    private ValidationIssue()
    {
    }

#pragma warning disable S107 // A finding is only useful with its full explanation.
    private ValidationIssue(
        Guid assessmentId,
        RuleMode mode,
        string ruleCode,
        string field,
        IssueSeverity severity,
        string message,
        string? expected,
        string? actual)
    {
        AssessmentId = assessmentId;
        Mode = mode;
        RuleCode = Truncate(ruleCode, 64) ?? ruleCode;
        Field = Truncate(field, 64) ?? field;
        Severity = severity;
        Message = Truncate(message, 512) ?? message;
        Expected = Truncate(expected, 256);
        Actual = Truncate(actual, 256);
    }
#pragma warning restore S107

    public Guid AssessmentId { get; private set; }

    public RuleMode Mode { get; private set; }

    public string RuleCode { get; private set; } = string.Empty;

    public string Field { get; private set; } = string.Empty;

    public IssueSeverity Severity { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public string? Expected { get; private set; }

    public string? Actual { get; private set; }

#pragma warning disable S107
    public static ValidationIssue Create(
        Guid assessmentId,
        RuleMode mode,
        string ruleCode,
        string field,
        IssueSeverity severity,
        string message,
        string? expected,
        string? actual) =>
        new(assessmentId, mode, ruleCode, field, severity, message, expected, actual);
#pragma warning restore S107

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
