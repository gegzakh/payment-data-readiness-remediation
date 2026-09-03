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
        await SeedReadinessReleaseAsync(cancellationToken);
        await SeedRemediationReleaseAsync(cancellationToken);
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

    private async Task SeedReadinessReleaseAsync(CancellationToken cancellationToken)
    {
        if (await ExistsAsync("0.3.0", cancellationToken))
        {
            return;
        }

        var release = Release.CreateDraft(
            "0.3.0",
            "Sources, ingestion and address readiness",
            DateOnly.FromDateTime(clock.UtcNow.UtcDateTime),
            "Payment party data can now be registered, ingested and assessed against today's and the post-cutover rules, with the payments at risk quantified.");

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Sources",
            "Source inventory with mappings, lineage and owner attestation",
            "Every system holding payment-party addresses is registered with its ISO 20022 field mappings, lineage, scan coverage and a named owner whose attestation goes stale on a configurable interval.",
            sortOrder: 0,
            references: ["FR-SRC-001", "FR-SRC-003", "FR-SRC-005", "FR-SRC-006"]);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Ingestion",
            "ISO 20022 XML and CSV ingestion with quarantine and reconciliation",
            "Uploads are size-, type- and content-checked before parsing; unsafe payloads are quarantined, duplicates are detected by content hash, and every batch reconciles input, parsed, duplicate, excluded and failed counts. Batches are idempotent, retryable and cancellable.",
            sortOrder: 1,
            references: ["FR-ING-001", "FR-ING-002", "FR-ING-004", "FR-ING-005", "FR-ING-006"]);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Validation",
            "Address classification and current versus post-cutover validation",
            "Each address is classified as structured, hybrid, unstructured, absent or unrecognized, then evaluated against the current and the future ruleset. Findings carry rule, field, severity, expectation, actual value and an evidence pointer back to the source record.",
            sortOrder: 2,
            references: ["FR-VAL-001", "FR-VAL-002", "FR-VAL-003", "FR-VAL-005"]);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Validation",
            "Portfolio readiness, exposure profiles and payments at risk",
            "Readiness today and after the cutover, the payments at risk and the dominant issues are aggregated across the latest run of every batch, and broken down by scheme, source, party role, country, classification or issue.",
            sortOrder: 3,
            references: ["FR-VAL-006", "FR-VAL-010"]);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Admin UI",
            "Readiness, sources and ingestion screens",
            "Operators upload and monitor batches, run validation, and drill from portfolio readiness into individual records; address detail stays masked unless the user holds the drill-down permission.",
            sortOrder: 4,
            references: ["FR-VAL-008"]);

        release.AddEntry(
            ReleaseEntryType.Fix,
            "Validation",
            "Exposure profiles count the same population as the readiness tiles",
            "Profile rows now cover the latest run per batch and assessed records only, report warnings alongside rejections, and score readiness per issue instead of showing zero.",
            sortOrder: 5,
            references: ["FR-VAL-006"]);

        release.AddEntry(
            ReleaseEntryType.Fix,
            "Ingestion",
            "A re-upload without an idempotency key is refused as a duplicate",
            "Replay protection is opt-in: only a caller-supplied idempotency key returns the original batch, so an unintended re-upload is answered by the duplicate check instead of silently succeeding.",
            sortOrder: 6,
            references: ["FR-ING-005"]);

        release.AddEntry(
            ReleaseEntryType.Fix,
            "Platform",
            "Authentication and permission failures return a problem document",
            "401 and 403 responses now carry the same ProblemDetails body, correlation id and error code as every other failure instead of an empty response.",
            sortOrder: 7,
            references: null);

        release.Publish("system", clock.UtcNow);

        context.Releases.Add(release);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRemediationReleaseAsync(CancellationToken cancellationToken)
    {
        if (await ExistsAsync("0.4.0", cancellationToken))
        {
            return;
        }

        var release = Release.CreateDraft(
            "0.4.0",
            "Remediation, approval and write-back",
            DateOnly.FromDateTime(clock.UtcNow.UtcDateTime),
            "Defective addresses become worked cases with evidence-backed corrections, an independent approval and a reversible write-back to the owning source.");

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Remediation",
            "One prioritised case per defective party address",
            "Validation findings are folded into a single case per party and address, however many payments they appeared in, carrying the original values, the failing rules, the affected schemes, the exposure after the cutover, the owning source and an SLA date. Priority combines rejection volume, scheme criticality, proximity to the cutover, recurrence and confidence.",
            sortOrder: 0,
            references: ["FR-REM-001", "FR-REM-002", "FR-REM-004"]);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Remediation",
            "Deterministic correction proposals with field-level confidence",
            "Corrections are parsed from the source values and approved reference data, and expose their method, per-field and overall confidence, ambiguities and alternatives. Anything machine-assisted is marked as needing human verification and can never be approved in bulk.",
            sortOrder: 1,
            references: ["FR-REM-005", "FR-REM-006"]);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Remediation",
            "Maker-checker workflow with evidence, exceptions and full history",
            "A maker edits the correction, attaches evidence and submits; a different person approves, returns, rejects, dismisses or grants a time-bound exception with a rationale. Corrections that add data the source never held require evidence, and expired exceptions stay visible as exposure rather than counting as compliant.",
            sortOrder: 2,
            references: ["FR-WF-001", "FR-WF-004", "FR-WF-005", "FR-WF-006"]);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Remediation",
            "Previewable bulk actions and customer campaigns",
            "Bulk submit, approve and assign show what they would touch — matched, eligible and blocked counts, the exposure, the lowest confidence, sample cases and why cases are held back — before anything is applied. Campaigns route cases to internal queues or corporate customers and track their progress.",
            sortOrder: 3,
            references: ["FR-REM-007", "FR-WF-007"]);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Write-back",
            "Reversible, idempotent write-back with read-after-write reconciliation",
            "Each source declares the fields it authorises, its API or export mode, maintenance window, per-run record limit and rollback method. Runs are previewed field by field, refuse stale records, carry an idempotency key and per-record correlation ids, read the record back to prove the correction landed, reconcile the counts and can be rolled back in full.",
            sortOrder: 4,
            references: ["FR-WB-001", "FR-WB-002", "FR-WB-004", "FR-WB-005", "FR-WB-007"]);

        release.AddEntry(
            ReleaseEntryType.Feature,
            "Admin UI",
            "Remediation queue and write-back screens",
            "The queue shows the funnel, filters and case detail with the original values beside the proposal; approvers record decisions, and operators preview, apply, reconcile and roll back write-back runs.",
            sortOrder: 5,
            references: null);

        release.Publish("system", clock.UtcNow);

        context.Releases.Add(release);
        await context.SaveChangesAsync(cancellationToken);
    }

    private Task<bool> ExistsAsync(string version, CancellationToken cancellationToken) =>
        context.Releases.AnyAsync(release => release.Version == version, cancellationToken);
}
