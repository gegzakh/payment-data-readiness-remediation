using System.Globalization;
using System.Text.RegularExpressions;
using PDR.Validation.Application.Upstream;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Application.Assess;

/// <summary>One finding produced by a rule against one field.</summary>
public sealed record RuleFinding(
    string RuleCode,
    string Field,
    IssueSeverity Severity,
    string Message,
    string? Expected,
    string? Actual);

/// <summary>
/// Applies a scheme rule set to one address deterministically (FR-VAL-005). Rules address a field by
/// name, so the evaluator resolves the field first and then runs the configured check.
/// </summary>
public static class RuleEvaluator
{
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromMilliseconds(200);

    public static IReadOnlyList<RuleFinding> Evaluate(
        AddressSnapshot address,
        IReadOnlyList<RuleSnapshot> rules)
    {
        var findings = new List<RuleFinding>();

        foreach (var rule in rules)
        {
            var value = ResolveField(address, rule.Field);
            var finding = Check(rule, value, address);
            if (finding is not null)
            {
                findings.Add(finding);
            }
        }

        return findings;
    }

    private static RuleFinding? Check(RuleSnapshot rule, string? value, AddressSnapshot address) => rule.Kind switch
    {
        RuleCheck.Required when string.IsNullOrWhiteSpace(value) =>
            Finding(rule, "a value", "missing"),
        RuleCheck.MaxLength when value is not null && ParseLength(rule.Parameter) is { } max && value.Length > max =>
            Finding(rule, $"at most {max} characters", $"{value.Length} characters"),
        RuleCheck.Pattern when !string.IsNullOrWhiteSpace(value) && !Matches(rule.Parameter, value) =>
            Finding(rule, rule.Parameter, value),
        RuleCheck.AllowedValues when !string.IsNullOrWhiteSpace(value) && !IsAllowed(rule.Parameter, value) =>
            Finding(rule, rule.Parameter, value),
        RuleCheck.Prohibited when !string.IsNullOrWhiteSpace(value) && ContainsProhibited(rule.Parameter, value) =>
            Finding(rule, $"no {rule.Parameter}", value),
        RuleCheck.StructuredOnly when address.Classification
            is AddressClassification.Unstructured or AddressClassification.Hybrid =>
            Finding(rule, "structured address elements", address.Classification.ToString()),
        _ => null
    };

    private static RuleFinding Finding(RuleSnapshot rule, string? expected, string? actual) =>
        new(rule.Code, rule.Field, rule.Severity, rule.Message, expected, actual);

    private static string? ResolveField(AddressSnapshot address, string field) =>
        field.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant() switch
        {
            "COUNTRY" => address.Country,
            "TOWNNAME" or "TOWN" => address.TownName,
            "POSTCODE" => address.PostCode,
            "STREETNAME" or "STREET" => address.StreetName,
            "BUILDINGNUMBER" => address.BuildingNumber,
            "ADDRESSLINE" or "ADDRESSLINES" => address.AddressLines,
            "PARTYNAME" => address.PartyName,
            _ => null
        };

    private static int? ParseLength(string? parameter) =>
        int.TryParse(parameter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var length) ? length : null;

    private static bool Matches(string? pattern, string value)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return true;
        }

        try
        {
            return Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant, PatternTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            // A malformed rule pattern must not fail the whole run; the rule simply cannot reject.
            return true;
        }
    }

    private static bool IsAllowed(string? parameter, string value) =>
        string.IsNullOrWhiteSpace(parameter) ||
        Split(parameter).Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    private static bool ContainsProhibited(string? parameter, string value) =>
        !string.IsNullOrWhiteSpace(parameter) &&
        Split(parameter).Exists(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));

    private static List<string> Split(string parameter) =>
        [.. parameter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}
