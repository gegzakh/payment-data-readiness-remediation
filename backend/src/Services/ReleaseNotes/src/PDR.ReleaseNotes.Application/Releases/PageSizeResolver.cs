using System.Globalization;
using PDR.BuildingBlocks.Core.Settings;

namespace PDR.ReleaseNotes.Application.Releases;

public static class ReleaseNotesSettingKeys
{
    public const string DefaultPageSize = "ReleaseNotes:Paging:DefaultPageSize";

    public const string AllowedPageSizes = "ReleaseNotes:Paging:AllowedPageSizes";

    public const string MaxPageSize = "ReleaseNotes:Paging:MaxPageSize";
}

/// <summary>
/// Resolves the effective page size from the runtime settings (database → appsettings), so operators can
/// switch the release-notes page between 10, 20, ... without a redeploy.
/// </summary>
public sealed class PageSizeResolver(ISettingsReader settings)
{
    public const int FallbackPageSize = 20;

    public const int FallbackMaxPageSize = 100;

    public static readonly int[] FallbackAllowedPageSizes = [10, 20, 50];

    public async Task<int> ResolveAsync(int? requested, CancellationToken cancellationToken = default)
    {
        var defaultPageSize = await settings.GetAsync(
            ReleaseNotesSettingKeys.DefaultPageSize,
            FallbackPageSize,
            cancellationToken);

        if (requested is null or <= 0)
        {
            return defaultPageSize;
        }

        var allowed = await GetAllowedPageSizesAsync(cancellationToken);
        if (allowed.Contains(requested.Value))
        {
            return requested.Value;
        }

        var maxPageSize = await settings.GetAsync(
            ReleaseNotesSettingKeys.MaxPageSize,
            FallbackMaxPageSize,
            cancellationToken);

        return Math.Min(requested.Value, maxPageSize);
    }

    public async Task<IReadOnlyList<int>> GetAllowedPageSizesAsync(CancellationToken cancellationToken = default)
    {
        var raw = await settings.GetAsync(ReleaseNotesSettingKeys.AllowedPageSizes, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return FallbackAllowedPageSizes;
        }

        var parsed = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size)
                ? size
                : 0)
            .Where(size => size > 0)
            .Distinct()
            .OrderBy(size => size)
            .ToArray();

        return parsed.Length == 0 ? FallbackAllowedPageSizes : parsed;
    }
}
