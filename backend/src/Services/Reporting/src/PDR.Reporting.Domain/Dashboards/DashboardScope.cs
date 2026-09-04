using System.Globalization;
using PDR.BuildingBlocks.Core.Guards;

namespace PDR.Reporting.Domain.Dashboards;

/// <summary>
/// The scope a dashboard was produced for. Two requests that mean the same thing must produce the same
/// key, otherwise the snapshot cache would silently serve numbers for a different population (FR-RPT-002).
/// </summary>
public sealed record DashboardScope(
    string? SchemeCodes,
    string? SourceCodes,
    string? Countries,
    string? Exclusions,
    DateOnly? AsOf)
{
    public static readonly DashboardScope All = new(null, null, null, null, null);

    public static DashboardScope Create(
        string? schemeCodes,
        string? sourceCodes,
        string? countries,
        string? exclusions,
        DateOnly? asOf) =>
        new(Normalize(schemeCodes), Normalize(sourceCodes), Normalize(countries), Normalize(exclusions), asOf);

    public string Key =>
        string.Join(
            '|',
            SchemeCodes ?? "*",
            SourceCodes ?? "*",
            Countries ?? "*",
            Exclusions ?? "-",
            AsOf?.ToString("O", CultureInfo.InvariantCulture) ?? "latest");

    public string Description
    {
        get
        {
            var parts = new List<string>();
            if (SchemeCodes is not null)
            {
                parts.Add($"schemes {SchemeCodes}");
            }

            if (SourceCodes is not null)
            {
                parts.Add($"sources {SourceCodes}");
            }

            if (Countries is not null)
            {
                parts.Add($"countries {Countries}");
            }

            if (Exclusions is not null)
            {
                parts.Add($"excluding {Exclusions}");
            }

            if (AsOf is not null)
            {
                parts.Add($"as of {AsOf:yyyy-MM-dd}");
            }

            return parts.Count == 0 ? "Whole portfolio" : string.Join(", ", parts);
        }
    }

    /// <summary>Filters a dimension key against the scope; an empty filter keeps everything.</summary>
    public bool Includes(string dimension, string key)
    {
        var filter = dimension switch
        {
            "Scheme" => SchemeCodes,
            "Source" => SourceCodes,
            "Country" => Countries,
            _ => null
        };

        if (Exclusions is not null && Exclusions.Split(',').Contains(key, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return filter is null || filter.Split(',').Contains(key, StringComparer.OrdinalIgnoreCase);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var items = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return items.Length == 0 ? null : Ensure.MaxLength(string.Join(',', items), 512);
    }
}
