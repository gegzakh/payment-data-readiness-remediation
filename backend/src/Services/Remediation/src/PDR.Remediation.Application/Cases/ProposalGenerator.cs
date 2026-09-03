using System.Globalization;
using System.Text.RegularExpressions;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Application.Cases;

/// <summary>
/// Turns the unstructured address a source holds into a structured proposal, deterministically: the same
/// input always yields the same output, every field carries the confidence its evidence justifies, and
/// anything the parser cannot resolve is reported as ambiguity rather than guessed (FR-REM-003, FR-REM-004).
/// Nothing here invents data — a field the input does not contain stays empty at zero confidence.
/// </summary>
public static partial class ProposalGenerator
{
    private const decimal FromSource = 100m;
    private const decimal WellFormed = 90m;
    private const decimal Parsed = 70m;
    private const decimal Weak = 40m;

    /// <summary>Builds a proposal from the values the source already holds plus its address lines.</summary>
    public static ProposedAddress Propose(OriginalAddress original, IReadOnlyDictionary<string, string>? countryPostCodePatterns = null)
    {
        var lines = SplitLines(original.AddressLines);
        var ambiguities = new List<string>();
        var alternatives = new List<string>();

        var (country, countryConfidence) = ResolveCountry(original.Country, lines, ambiguities);
        var (postCode, postCodeConfidence) = ResolvePostCode(
            original.PostCode,
            lines,
            country,
            countryPostCodePatterns,
            ambiguities,
            alternatives);

        var (street, buildingNumber, streetConfidence, buildingConfidence) =
            ResolveStreet(original.StreetName, original.BuildingNumber, lines, ambiguities);

        var (town, townConfidence) = ResolveTown(original.TownName, lines, postCode, ambiguities);

        return new ProposedAddress(
            country,
            town,
            postCode,
            street,
            buildingNumber,
            new FieldConfidence(countryConfidence, townConfidence, postCodeConfidence, streetConfidence, buildingConfidence),
            ambiguities.Count == 0 ? null : string.Join("; ", ambiguities),
            alternatives.Count == 0 ? null : string.Join("; ", alternatives));
    }

    private static IReadOnlyList<string> SplitLines(string? addressLines) =>
        string.IsNullOrWhiteSpace(addressLines)
            ? []
            : [.. addressLines
                .Split(['\n', '|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => line.Length > 0)];

    private static (string? Value, decimal Confidence) ResolveCountry(
        string? country,
        IReadOnlyList<string> lines,
        List<string> ambiguities)
    {
        if (IsCountryCode(country))
        {
            return (country!.Trim().ToUpperInvariant(), FromSource);
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            var mapped = CountryNames.GetValueOrDefault(Normalize(country));
            if (mapped is not null)
            {
                return (mapped, WellFormed);
            }
        }

        // The last line of a postal address is conventionally the country.
        foreach (var candidate in lines.Reverse())
        {
            if (IsCountryCode(candidate))
            {
                return (candidate.Trim().ToUpperInvariant(), Parsed);
            }

            var mapped = CountryNames.GetValueOrDefault(Normalize(candidate));
            if (mapped is not null)
            {
                return (mapped, Parsed);
            }
        }

        ambiguities.Add("The country could not be derived from the source values or the address lines.");
        return (null, 0m);
    }

    private static (string? Value, decimal Confidence) ResolvePostCode(
        string? postCode,
        IReadOnlyList<string> lines,
        string? country,
        IReadOnlyDictionary<string, string>? patterns,
        List<string> ambiguities,
        List<string> alternatives)
    {
        if (!string.IsNullOrWhiteSpace(postCode))
        {
            return (postCode.Trim(), FromSource);
        }

        var pattern = country is not null ? patterns?.GetValueOrDefault(country) : null;
        var matches = new List<string>();

        foreach (var line in lines)
        {
            foreach (Match match in PostCodeCandidate().Matches(line))
            {
                var value = match.Value.Trim();
                if (pattern is null || Regex.IsMatch(value, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200)))
                {
                    matches.Add(value);
                }
            }
        }

        if (matches.Count == 0)
        {
            ambiguities.Add("No postal code was found in the address lines.");
            return (null, 0m);
        }

        if (matches.Count > 1)
        {
            alternatives.AddRange(matches.Skip(1).Select(match => $"postCode={match}"));
            ambiguities.Add($"{matches.Count} postal-code candidates were found; the first was proposed.");
            return (matches[0], Weak);
        }

        return (matches[0], pattern is null ? Parsed : WellFormed);
    }

    private static (string? Street, string? BuildingNumber, decimal StreetConfidence, decimal BuildingConfidence)
        ResolveStreet(
            string? street,
            string? buildingNumber,
            IReadOnlyList<string> lines,
            List<string> ambiguities)
    {
        var haveStreet = !string.IsNullOrWhiteSpace(street);
        var haveNumber = !string.IsNullOrWhiteSpace(buildingNumber);

        if (haveStreet && haveNumber)
        {
            return (street!.Trim(), buildingNumber!.Trim(), FromSource, FromSource);
        }

        var candidate = haveStreet ? street!.Trim() : lines.FirstOrDefault(HasStreetShape);

        if (candidate is null)
        {
            ambiguities.Add("No street could be identified in the address lines.");
            return (
                haveStreet ? street!.Trim() : null,
                haveNumber ? buildingNumber!.Trim() : null,
                haveStreet ? FromSource : 0m,
                haveNumber ? FromSource : 0m);
        }

        var match = StreetAndNumber().Match(candidate);
        if (!match.Success)
        {
            ambiguities.Add($"The building number could not be separated from '{candidate}'.");
            return (candidate, haveNumber ? buildingNumber!.Trim() : null, Weak, haveNumber ? FromSource : 0m);
        }

        var parsedStreet = match.Groups["street"].Value.Trim(' ', ',');
        var parsedNumber = match.Groups["number"].Value.Trim();

        return (
            haveStreet ? street!.Trim() : parsedStreet,
            haveNumber ? buildingNumber!.Trim() : parsedNumber,
            haveStreet ? FromSource : Parsed,
            haveNumber ? FromSource : Parsed);
    }

    private static (string? Value, decimal Confidence) ResolveTown(
        string? town,
        IReadOnlyList<string> lines,
        string? postCode,
        List<string> ambiguities)
    {
        if (!string.IsNullOrWhiteSpace(town))
        {
            return (town.Trim(), FromSource);
        }

        // "12345 Berlin" or "Berlin 12345": the town is whatever sits beside the postal code.
        if (postCode is not null)
        {
            foreach (var line in lines.Where(line => line.Contains(postCode, StringComparison.OrdinalIgnoreCase)))
            {
                var remainder = line
                    .Replace(postCode, string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Trim(' ', ',', '-');

                if (remainder.Length > 1 && !HasStreetShape(remainder))
                {
                    return (Titleize(remainder), Parsed);
                }
            }
        }

        var fallback = lines.LastOrDefault(line =>
            !HasStreetShape(line) && !IsCountryCode(line) && CountryNames.GetValueOrDefault(Normalize(line)) is null);

        if (fallback is null)
        {
            ambiguities.Add("No town could be identified in the address lines.");
            return (null, 0m);
        }

        return (Titleize(fallback), Weak);
    }

    private static bool IsCountryCode(string? value) =>
        value is not null && CountryCode().IsMatch(value.Trim());

    private static bool HasStreetShape(string line) => StreetAndNumber().IsMatch(line);

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static string Titleize(string value) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Trim().ToLowerInvariant());

    private static readonly Dictionary<string, string> CountryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GERMANY"] = "DE",
        ["DEUTSCHLAND"] = "DE",
        ["FRANCE"] = "FR",
        ["NETHERLANDS"] = "NL",
        ["THE NETHERLANDS"] = "NL",
        ["SPAIN"] = "ES",
        ["ITALY"] = "IT",
        ["BELGIUM"] = "BE",
        ["AUSTRIA"] = "AT",
        ["POLAND"] = "PL",
        ["PORTUGAL"] = "PT",
        ["IRELAND"] = "IE",
        ["UNITED KINGDOM"] = "GB",
        ["GREAT BRITAIN"] = "GB",
        ["UNITED STATES"] = "US",
        ["USA"] = "US",
        ["SWITZERLAND"] = "CH",
        ["ARMENIA"] = "AM"
    };

    [GeneratedRegex(@"^[A-Za-z]{2}$")]
    private static partial Regex CountryCode();

    [GeneratedRegex(@"(?<street>[\p{L}\.\-' ]{3,})\s+(?<number>\d+[A-Za-z]?(?:[/-]\d+[A-Za-z]?)?)\s*$")]
    private static partial Regex StreetAndNumber();

    [GeneratedRegex(@"\b(?:[A-Z]{1,2}\d{1,2}[A-Z]?\s?\d[A-Z]{2}|\d{4,6}(?:-\d{4})?)\b")]
    private static partial Regex PostCodeCandidate();
}
