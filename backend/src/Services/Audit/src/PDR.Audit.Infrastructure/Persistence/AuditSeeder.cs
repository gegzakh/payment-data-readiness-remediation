using Microsoft.EntityFrameworkCore;
using PDR.Audit.Application.Ledger;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.BuildingBlocks.Persistence.Settings;

namespace PDR.Audit.Infrastructure.Persistence;

/// <summary>Seeds the ledger's runtime tunables; audit records themselves are never seeded.</summary>
public sealed class AuditSeeder(AuditDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new (string Key, string Value, string Type, string Description)[]
        {
            (AuditSettingKeys.RetentionYears, "10", "int", "Years audit records are retained before archival."),
            (AuditSettingKeys.MaxPageSize, "200", "int", "Upper bound applied to audit search page sizes."),
            (AuditSettingKeys.VerifyBatchSize, "500", "int", "Records re-hashed per batch during chain verification.")
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
