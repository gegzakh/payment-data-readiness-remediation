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
        await SeedFirstReleaseAsync(cancellationToken);
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

    private async Task SeedFirstReleaseAsync(CancellationToken cancellationToken)
    {
        if (await context.Releases.AnyAsync(cancellationToken))
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
}
