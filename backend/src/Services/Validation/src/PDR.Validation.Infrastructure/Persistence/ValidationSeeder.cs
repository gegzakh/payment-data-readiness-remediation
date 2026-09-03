using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.BuildingBlocks.Persistence.Settings;
using PDR.Validation.Application.Assess;
using PDR.Validation.Application.Assess.Queries;

namespace PDR.Validation.Infrastructure.Persistence;

/// <summary>Seeds the validation tunables; assessments only ever come from real runs.</summary>
public sealed class ValidationSeeder(ValidationDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new (string Key, string Value, string Type, string Description)[]
        {
            (ValidationSettingKeys.PageSize,
                ValidationDefaults.PageSize.ToString(),
                "int",
                "Default page size for validation run and record listings."),
            (ValidationSettingKeys.DefaultSchemeCode,
                ValidationDefaults.SchemeCode,
                "string",
                "Scheme whose rule sets are used when a batch carries no scheme."),
            (ValidationSettingKeys.TopIssueCount,
                ValidationDefaults.TopIssueCount.ToString(),
                "int",
                "How many rule findings the readiness summary lists."),
            (ValidationSettingKeys.FutureAsOfDate,
                "2026-11-15",
                "string",
                "Date the post-cutover rule set is evaluated against by default.")
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
