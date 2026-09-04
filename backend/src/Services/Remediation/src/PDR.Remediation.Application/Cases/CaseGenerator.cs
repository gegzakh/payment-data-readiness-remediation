using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.Remediation.Application.Abstractions;
using PDR.Remediation.Application.Upstream;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Application.Cases;

/// <summary>
/// Turns a validation run into remediation work: one case per defective address per source, with the
/// repeat payments folded into it as occurrences rather than duplicated (FR-REM-001). Each new case gets
/// a deterministic proposal, a priority derived from what it costs to leave broken, a queue and an SLA.
/// </summary>
public sealed class CaseGenerator(
    IRemediationDbContext context,
    IValidationGateway validation,
    ISourcesGateway sources,
    ISettingsReader settings,
    IClock clock,
    ILogger<CaseGenerator> logger)
{
    public async Task<Result<CaseGenerationDto>> GenerateAsync(Guid? runId, CancellationToken cancellationToken)
    {
        ValidationRunSummary? run;
        IReadOnlyList<AssessedAddress> assessments;

        try
        {
            run = runId is { } id
                ? await validation.GetRunAsync(id, cancellationToken)
                : await validation.GetLatestRunAsync(cancellationToken);

            if (run is null)
            {
                return Result.Failure<CaseGenerationDto>(RemediationErrors.RunNotFound(runId ?? Guid.Empty));
            }

            assessments = await validation.GetAssessmentsAsync(run.Id, cancellationToken);
        }
        catch (UpstreamException exception)
        {
            logger.LogWarning(exception, "Validation could not be read for run {RunId}.", runId);
            return Result.Failure<CaseGenerationDto>(RemediationErrors.UpstreamUnavailable("validation"));
        }

        var defective = assessments
            .Where(assessment => assessment.Issues.Count > 0
                                 && assessment.FutureOutcome is "Rejected" or "Warning")
            .ToList();

        var queue = await settings.GetAsync(RemediationSettingKeys.DefaultQueue, RemediationDefaults.DefaultQueue, cancellationToken);
        var slaDays = await settings.GetAsync(RemediationSettingKeys.SlaDays, RemediationDefaults.DefaultSlaDays, cancellationToken);
        var criticalSchemes = (await settings.GetAsync(
                RemediationSettingKeys.CriticalSchemes,
                RemediationDefaults.CriticalSchemes,
                cancellationToken))
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(scheme => scheme.ToUpperInvariant())
            .ToHashSet();

        var cutover = await ResolveCutoverAsync(cancellationToken);
        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var daysToCutover = cutover.DayNumber - today.DayNumber;

        var created = 0;
        var updated = 0;
        var folded = 0;
        var owners = new Dictionary<string, SourceOwner?>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in defective.GroupBy(CaseKeyOf, StringComparer.Ordinal))
        {
            var first = group.First();
            var occurrences = group.Count();
            var futureExposure = group.Count(assessment => assessment.FutureOutcome == "Rejected");

            var existing = await context.Cases
                .Include(entity => entity.Evidence)
                .FirstOrDefaultAsync(entity => entity.CaseKey == group.Key, cancellationToken);

            if (existing is not null)
            {
                if (existing.Status is CaseStatus.Remediated or CaseStatus.Dismissed or CaseStatus.Rejected)
                {
                    continue;
                }

                existing.RecordOccurrences(occurrences, futureExposure, first.EvidencePointer);
                existing.Prioritize(daysToCutover, IsCritical(group, criticalSchemes));
                folded += occurrences;
                updated++;
                continue;
            }

            if (!owners.TryGetValue(first.SourceCode, out var owner))
            {
                owner = await TryGetOwnerAsync(first.SourceCode, cancellationToken);
                owners[first.SourceCode] = owner;
            }

            var original = new OriginalAddress(
                first.Country,
                first.TownName,
                first.PostCode,
                first.StreetName,
                first.BuildingNumber,
                first.AddressLines);

            var subject = new CaseSubject(
                group.Key,
                first.SourceCode,
                owner?.OwnerName,
                owner?.OwnerEmail,
                first.PartyName,
                first.PartyRole,
                original,
                string.Join(
                    ',',
                    group.SelectMany(assessment => assessment.Issues)
                        .Select(issue => issue.RuleCode)
                        .Distinct(StringComparer.Ordinal)
                        .Order(StringComparer.Ordinal)),
                string.Join(
                    ',',
                    group.Select(assessment => assessment.SchemeCode ?? "UNKNOWN")
                        .Distinct(StringComparer.Ordinal)
                        .Order(StringComparer.Ordinal)),
                first.EvidencePointer);

            var remediationCase = RemediationCase.Open(subject, now);
            remediationCase.RecordOccurrences(occurrences, futureExposure, first.EvidencePointer);
            remediationCase.Prioritize(daysToCutover, IsCritical(group, criticalSchemes));

            var proposal = ProposalGenerator.Propose(original);
            remediationCase.Propose(
                ProposalMethod.DeterministicParse,
                proposal,
                "Generated from the source values and address lines.",
                "system",
                now);

            remediationCase.Assign(queue, assignedTo: null, today.AddDays(slaDays), "system", now);

            context.Cases.Add(remediationCase);
            created++;
            folded += occurrences;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Generated remediation work from run {RunId}: {Created} new cases, {Updated} updated, {Folded} occurrences.",
            run.Id,
            created,
            updated,
            folded);

        return new CaseGenerationDto(run.Id, assessments.Count, created, updated, folded, now);
    }

    private async Task<SourceOwner?> TryGetOwnerAsync(string sourceCode, CancellationToken cancellationToken)
    {
        try
        {
            return await sources.GetOwnerAsync(sourceCode, cancellationToken);
        }
        catch (UpstreamException exception)
        {
            // A missing steward must not stop remediation; the case is still actionable without it.
            logger.LogWarning(exception, "The owner of source {SourceCode} could not be read.", sourceCode);
            return null;
        }
    }

    private async Task<DateOnly> ResolveCutoverAsync(CancellationToken cancellationToken)
    {
        var configured = await settings.GetAsync(
            RemediationSettingKeys.CutoverDate,
            RemediationDefaults.CutoverDate,
            cancellationToken);

        return DateOnly.TryParse(configured, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : DateOnly.Parse(RemediationDefaults.CutoverDate, CultureInfo.InvariantCulture);
    }

    private static bool IsCritical(IEnumerable<AssessedAddress> group, HashSet<string> criticalSchemes) =>
        group.Any(assessment => assessment.SchemeCode is { } scheme && criticalSchemes.Contains(scheme.ToUpperInvariant()));

    /// <summary>
    /// The identity of the defect: the same party address in the same source is one case however many
    /// payments carried it. Hashing keeps personal data out of the key while staying deterministic.
    /// </summary>
    internal static string CaseKeyOf(AssessedAddress assessment)
    {
        var material = string.Join(
            '\u001f',
            assessment.SourceCode.ToUpperInvariant(),
            assessment.PartyRole.ToString(),
            Normalize(assessment.PartyName),
            Normalize(assessment.Country),
            Normalize(assessment.TownName),
            Normalize(assessment.PostCode),
            Normalize(assessment.StreetName),
            Normalize(assessment.BuildingNumber),
            Normalize(assessment.AddressLines));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return $"{assessment.SourceCode.ToUpperInvariant()}:{Convert.ToHexStringLower(hash)[..32]}";
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
}
