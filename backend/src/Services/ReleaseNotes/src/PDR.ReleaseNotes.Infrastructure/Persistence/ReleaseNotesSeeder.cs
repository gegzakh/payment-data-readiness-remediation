using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.BuildingBlocks.Persistence.Settings;
using PDR.ReleaseNotes.Application.Releases;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.Infrastructure.Persistence;

/// <summary>
/// Seeds the runtime paging settings and the platform's first release note, so a fresh environment
/// renders a populated release-notes page.
/// </summary>
public sealed class ReleaseNotesSeeder(ReleaseNotesDbContext context, IClock clock) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedSettingsAsync(cancellationToken);
        await SeedFoundationReleaseAsync(cancellationToken);
        await SeedRulesAndAuditReleaseAsync(cancellationToken);
    }

    private async Task SeedSettingsAsync(CancellationToken cancellationToken)
    {
        var defaults = new (string Key, string Value, string Type, string Description)[]
        {
            (ReleaseNotesSettingKeys.DefaultPageSize, "20", "int", "Release notes per page when the caller does not specify one."),
            (ReleaseNotesSettingKeys.AllowedPageSizes, "10,20,50", "string", "Page sizes offered by the release-notes page."),
            (ReleaseNotesSettingKeys.MaxPageSize, "100", "int", "Upper bound applied to any requested page size.")
        };

        foreach (var (key, value, type, description) in defaults)
        {
            var exists = await context.SystemSettings.AnyAsync(setting => setting.Key == key, cancellationToken);
            if (!exists)
            {
                context.SystemSettings.Add(new SystemSetting(key, value, type, description));
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedFoundationReleaseAsync(CancellationToken cancellationToken)
    {
        if (await ExistsAsync("0.1.0", cancellationToken))
        {
            return;
        }

        var release = Release.CreateDraft(
            "0.1.0",
            "Platform foundation",
            DateOnly.FromDateTime(clock.UtcNow.UtcDateTime),
            "First slice of the payment data readiness platform: shared service foundation and release notes.");

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Platform",
            "Shared service foundation",
            "Common error handling, logging, correlation, authentication and automatic database migration for every service.",
            sortOrder: 0,
            references: null);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Release Notes",
            "Release notes service and page",
            "Releases and entries are authored, published and served newest-first with configurable pagination.",
            sortOrder: 1,
            references: null);

        release.Publish("system", clock.UtcNow);

        context.Releases.Add(release);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRulesAndAuditReleaseAsync(CancellationToken cancellationToken)
    {
        if (await ExistsAsync("0.2.0", cancellationToken))
        {
            return;
        }

        var release = Release.CreateDraft(
            "0.2.0",
            "Scheme rules and evidential audit",
            DateOnly.FromDateTime(clock.UtcNow.UtcDateTime),
            "Validation rules become versioned data a scheme owner can change, and every change is recorded in a tamper-evident ledger.");

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Rules",
            "Versioned, dated scheme rulesets",
            "Rules are data: required, maximum length, pattern, allowed and prohibited values, structured-only. Versions are drafted, activated from a date and can be rolled back by re-activating an earlier one.",
            sortOrder: 0,
            references: ["FR-RUL-001", "FR-RUL-003", "FR-RUL-004"]);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Rules",
            "Scheme and country reference data",
            "Payment schemes carry their structured-address cutover date, and countries their postcode and SEPA attributes, so rules can be evaluated for current and post-cutover behaviour.",
            sortOrder: 1,
            references: ["FR-RUL-002"]);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Audit",
            "Append-only hash-chained audit ledger",
            "Every recorded action links to its predecessor by hash, is searchable by service, action, entity, actor, correlation id and time, and can be verified end to end to detect edits made outside the application.",
            sortOrder: 2,
            references: ["FR-AUD-001", "FR-AUD-002"]);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Admin UI",
            "Rules and audit administration screens",
            "Scheme owners author rule versions and activate or roll them back; auditors filter the ledger and verify chain integrity.",
            sortOrder: 3,
            references: null);

        release.Publish("system", clock.UtcNow);

        context.Releases.Add(release);
        await context.SaveChangesAsync(cancellationToken);
    }

    private Task<bool> ExistsAsync(string version, CancellationToken cancellationToken) =>
        context.Releases.AnyAsync(release => release.Version == version, cancellationToken);
}
