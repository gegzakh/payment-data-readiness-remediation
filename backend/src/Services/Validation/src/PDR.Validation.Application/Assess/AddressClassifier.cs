using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Application.Assess;

/// <summary>
/// Deterministic address classification (FR-VAL-003). It looks only at which elements carry content,
/// never at whether they satisfy a rule, so classification and validation stay independent.
/// </summary>
public static class AddressClassifier
{
    public static AddressClassification Classify(
        string? country,
        string? townName,
        string? postCode,
        string? streetName,
        string? buildingNumber,
        string? addressLines)
    {
        var structuredElements = new[] { townName, postCode, streetName, buildingNumber }
            .Count(value => !string.IsNullOrWhiteSpace(value));

        var hasCountry = !string.IsNullOrWhiteSpace(country);
        var hasLines = !string.IsNullOrWhiteSpace(addressLines);

        if (structuredElements == 0 && !hasLines)
        {
            return hasCountry ? AddressClassification.Unrecognized : AddressClassification.Absent;
        }

        if (structuredElements > 0 && hasLines)
        {
            return AddressClassification.Hybrid;
        }

        if (structuredElements > 0)
        {
            return hasCountry ? AddressClassification.Structured : AddressClassification.Unrecognized;
        }

        return LooksLikeAddress(addressLines!)
            ? AddressClassification.Unstructured
            : AddressClassification.Unrecognized;
    }

    /// <summary>
    /// Free text is only treated as an address when it carries more than a single token; a stray word or
    /// punctuation is content the programme cannot interpret and must be reported as unrecognized.
    /// </summary>
    private static bool LooksLikeAddress(string addressLines)
    {
        var tokens = addressLines
            .Split([' ', ',', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Any(char.IsLetterOrDigit))
            .ToList();

        return tokens.Count >= 2;
    }
}
