using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.BuildingBlocks.Persistence.Settings;
using PDR.Ingestion.Application.Ingest;

namespace PDR.Ingestion.Infrastructure.Persistence;

/// <summary>Seeds the ingestion tunables; batch data only ever comes from real submissions.</summary>
public sealed class IngestionSeeder(IngestionDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new (string Key, string Value, string Type, string Description)[]
        {
            (IngestionSettingKeys.MaxFileBytes,
                FileSafetyInspector.DefaultMaxFileBytes.ToString(),
                "long",
                "Largest payload accepted for ingestion, in bytes."),
            (IngestionSettingKeys.AllowedExtensions,
                FileSafetyInspector.DefaultAllowedExtensions,
                "string",
                "Comma separated list of accepted file extensions."),
            (IngestionSettingKeys.MaxRecords,
                FileSafetyInspector.DefaultMaxRecords.ToString(),
                "int",
                "Maximum number of records or parties accepted in a single payload."),
            (IngestionSettingKeys.CsvDelimiter,
                FileSafetyInspector.DefaultCsvDelimiter,
                "string",
                "Field delimiter used by the approved delimited layout."),
            (IngestionSettingKeys.DefaultSchemeCode,
                "SEPA",
                "string",
                "Scheme assigned to ingested records when the payload does not carry one."),
            (IngestionSettingKeys.PageSize,
                "20",
                "int",
                "Default page size for batch and record listings.")
        };

        foreach (var (key, value, type, description) in defaults)
        {
            if (!await context.SystemSettings.AnyAsync(setting => setting.Key == key, cancellationToken))
            {
                context.SystemSettings.Add(new SystemSetting(key, value, type, description));
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
