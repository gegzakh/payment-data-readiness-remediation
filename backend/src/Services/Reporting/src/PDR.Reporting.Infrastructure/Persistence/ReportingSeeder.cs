using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.BuildingBlocks.Persistence.Settings;
using PDR.Reporting.Application.Dashboards;

namespace PDR.Reporting.Infrastructure.Persistence;

/// <summary>Seeds only the tunables; every snapshot is produced from real upstream data.</summary>
public sealed class ReportingSeeder(ReportingDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new (string Key, string Value, string Type, string Description)[]
        {
            (ReportingSettingKeys.FreshnessSeconds,
                ReportingDefaults.FreshnessSeconds.ToString(CultureInfo.InvariantCulture),
                "int",
                "How long a captured dashboard snapshot is reused before it is rebuilt from upstream."),
            (ReportingSettingKeys.HistoryPageSize,
                ReportingDefaults.HistoryPageSize.ToString(CultureInfo.InvariantCulture),
                "int",
                "Default page size for the snapshot history listing.")
        };

        var added = false;
        foreach (var (key, value, type, description) in defaults)
        {
            if (!await context.SystemSettings.AnyAsync(setting => setting.Key == key, cancellationToken))
            {
                context.SystemSettings.Add(new SystemSetting(key, value, type, description));
                added = true;
            }
        }

        if (added)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
