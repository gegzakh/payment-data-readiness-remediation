using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.Remediation.Application.Abstractions;
using PDR.Remediation.Domain.Cases;
using PDR.Remediation.Domain.WriteBack;

namespace PDR.Remediation.Application.WriteBack;

/// <summary>
/// Applies approved corrections to a source system: refuses stale updates, replays idempotently, reads
/// back what it wrote, and keeps the before value so the change can be reversed
/// (FR-WB-002, FR-WB-003, FR-WB-005, FR-WB-007, FR-WB-008).
/// </summary>
public sealed class WriteBackService(
    IRemediationDbContext context,
    IWriteBackConnector connector,
    ISettingsReader settings,
    ICurrentUser currentUser,
    IClock clock,
    ILogger<WriteBackService> logger)
{
    public async Task<Result<WriteBackPreviewDto>> PreviewAsync(
        string sourceCode,
        IReadOnlyList<Guid>? caseIds,
        CancellationToken cancellationToken)
    {
        var target = await FindTargetAsync(sourceCode, cancellationToken);
        if (target is null)
        {
            return Result.Failure<WriteBackPreviewDto>(WriteBackErrors.TargetNotConfigured(sourceCode));
        }

        var cases = await EligibleCasesAsync(target.SourceCode, caseIds, target.MaxRecordsPerRun, cancellationToken);
        var blockers = new List<string>();
        var changes = new List<WriteBackChangeDto>();

        foreach (var entity in cases)
        {
            foreach (var (field, before, after) in FieldChanges(entity))
            {
                if (!target.Allows(field))
                {
                    blockers.Add($"{entity.CaseKey}: the source does not accept '{field}'.");
                    continue;
                }

                changes.Add(new WriteBackChangeDto(entity.Id, RecordReference(entity), field, before, after));
            }
        }

        return new WriteBackPreviewDto(
            target.SourceCode,
            target.Mode,
            target.MaintenanceWindow,
            target.MaxRecordsPerRun,
            target.RollbackMethod,
            cases.Count,
            changes.Select(change => change.RecordReference).Distinct(StringComparer.Ordinal).Count(),
            changes,
            blockers);
    }

    public async Task<Result<WriteBackJobDto>> ApplyAsync(
        string sourceCode,
        IReadOnlyList<Guid>? caseIds,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var target = await FindTargetAsync(sourceCode, cancellationToken);
        if (target is null || !target.IsEnabled)
        {
            return Result.Failure<WriteBackJobDto>(WriteBackErrors.TargetNotConfigured(sourceCode));
        }

        var key = string.IsNullOrWhiteSpace(idempotencyKey)
            ? $"{target.SourceCode}:{Guid.CreateVersion7():n}"
            : idempotencyKey.Trim();

        // Replaying a key returns the original job rather than writing to the source twice (FR-WB-003).
        var replay = await context.WriteBackJobs
            .Include(job => job.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.IdempotencyKey == key, cancellationToken);

        if (replay is not null)
        {
            return replay.ToDto();
        }

        var cases = await EligibleCasesAsync(target.SourceCode, caseIds, target.MaxRecordsPerRun, cancellationToken);
        if (cases.Count == 0)
        {
            return Result.Failure<WriteBackJobDto>(WriteBackErrors.NoEligibleCases);
        }

        var now = clock.UtcNow;
        var job = WriteBackJob.Create(target.SourceCode, target.Mode, key, currentUser.UserName, now);
        context.WriteBackJobs.Add(job);

        var readBack = await settings.GetAsync(
            WriteBackSettingKeys.ReadBackAfterWrite,
            WriteBackDefaults.ReadBackAfterWrite,
            cancellationToken);

        var confirmed = new List<Guid>();
        var payload = new StringBuilder();

        foreach (var entity in cases)
        {
            var reference = RecordReference(entity);
            var before = WriteBackMapper.RenderOriginal(entity);
            var after = WriteBackMapper.RenderProposed(entity);
            var item = job.AddItem(entity.Id, reference, await VersionAsync(target.SourceCode, reference, cancellationToken), before, after);

            var queued = entity.QueueForWriteBack(currentUser.UserName, now);
            if (queued.IsFailure)
            {
                item.Fail(queued.Error.Message);
                continue;
            }

            var correlationId = Guid.CreateVersion7().ToString("n");
            var outcome = await connector.ApplyAsync(
                new WriteBackInstruction(target.SourceCode, reference, item.SourceVersion, after, correlationId),
                cancellationToken);

            if (!outcome.Succeeded)
            {
                if (outcome.ObservedVersion is { } observed && observed != item.SourceVersion)
                {
                    item.MarkStale(observed);
                    entity.MarkFailed("The source record changed after the correction was approved.", currentUser.UserName, now);
                }
                else
                {
                    item.Fail(outcome.Message ?? "The source rejected the correction.");
                    entity.MarkFailed(outcome.Message ?? "The source rejected the correction.", currentUser.UserName, now);
                }

                continue;
            }

            item.Apply(correlationId, now);
            payload.Append(CultureInfo.InvariantCulture, $"{reference}={after}\n");

            if (!readBack)
            {
                entity.MarkRemediated(currentUser.UserName, now);
                continue;
            }

            var observedValue = await connector.ReadBackAsync(target.SourceCode, reference, cancellationToken);
            if (string.Equals(observedValue, after, StringComparison.Ordinal))
            {
                confirmed.Add(item.Id);
                entity.MarkRemediated(currentUser.UserName, now);
            }
            else
            {
                item.Fail("Read-after-write did not return the corrected value.");
                entity.MarkFailed("The source did not report the corrected value on read-back.", currentUser.UserName, now);
            }
        }

        var checksum = target.Mode == WriteBackMode.Export
            ? Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToString())))
            : null;

        job.CompleteApply(now, checksum);

        if (confirmed.Count > 0)
        {
            var confirmation = job.Confirm(confirmed, now);
            if (confirmation.IsFailure)
            {
                logger.LogWarning("Write-back job {JobId} could not be confirmed: {Error}.", job.Id, confirmation.Error.Message);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Write-back {JobId} to {SourceCode}: {Applied} applied, {Failed} failed, {Stale} stale.",
            job.Id,
            target.SourceCode,
            job.AppliedCount,
            job.FailedCount,
            job.StaleCount);

        return job.ToDto();
    }

    /// <summary>Reverses an applied job and reopens its cases, so nothing silently stays "remediated".</summary>
    public async Task<Result<WriteBackJobDto>> RollbackAsync(
        Guid jobId,
        string reason,
        CancellationToken cancellationToken)
    {
        var job = await context.WriteBackJobs
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.Id == jobId, cancellationToken);

        if (job is null)
        {
            return Result.Failure<WriteBackJobDto>(WriteBackErrors.JobNotFound(jobId));
        }

        var now = clock.UtcNow;
        var reversible = job.Items
            .Where(item => item.Status is WriteBackItemStatus.Applied or WriteBackItemStatus.Confirmed)
            .ToList();

        foreach (var item in reversible)
        {
            var outcome = await connector.RevertAsync(
                new WriteBackInstruction(
                    job.TargetSourceCode,
                    item.RecordReference,
                    null,
                    item.BeforeValue,
                    item.CorrelationId ?? Guid.CreateVersion7().ToString("n")),
                cancellationToken);

            if (!outcome.Succeeded)
            {
                return Result.Failure<WriteBackJobDto>(
                    Error.Dependency("WRITEBACK.ROLLBACK_FAILED", outcome.Message ?? "The source refused the reversal."));
            }
        }

        var result = job.Rollback(reason, now);
        if (result.IsFailure)
        {
            return Result.Failure<WriteBackJobDto>(result.Error);
        }

        var caseIds = reversible.Select(item => item.CaseId).ToList();
        var cases = await context.Cases
            .Include(entity => entity.History)
            .Where(entity => caseIds.Contains(entity.Id))
            .ToListAsync(cancellationToken);

        foreach (var entity in cases)
        {
            entity.MarkRolledBack(reason, currentUser.UserName, now);
        }

        await context.SaveChangesAsync(cancellationToken);
        return job.ToDto();
    }

    /// <summary>Every requested record must end in exactly one terminal bucket (FR-WB-006).</summary>
    public async Task<Result<WriteBackReconciliationDto>> ReconcileAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await context.WriteBackJobs
            .Include(entity => entity.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == jobId, cancellationToken);

        if (job is null)
        {
            return Result.Failure<WriteBackReconciliationDto>(WriteBackErrors.JobNotFound(jobId));
        }

        var discrepancies = new List<string>();
        var accounted = job.ConfirmedCount
                        + job.Items.Count(item => item.Status == WriteBackItemStatus.Applied)
                        + job.FailedCount
                        + job.StaleCount
                        + job.RolledBackCount;

        if (accounted != job.ItemCount)
        {
            discrepancies.Add($"{job.ItemCount - accounted} record(s) are still pending an outcome.");
        }

        foreach (var item in job.Items.Where(item => item.Status == WriteBackItemStatus.Applied))
        {
            var observed = await connector.ReadBackAsync(job.TargetSourceCode, item.RecordReference, cancellationToken);
            if (!string.Equals(observed, item.AfterValue, StringComparison.Ordinal))
            {
                discrepancies.Add($"{item.RecordReference}: the source does not hold the corrected value.");
            }
        }

        return new WriteBackReconciliationDto(
            job.Id,
            job.ItemCount,
            job.AppliedCount,
            job.ConfirmedCount,
            job.FailedCount,
            job.StaleCount,
            job.RolledBackCount,
            discrepancies.Count == 0,
            discrepancies);
    }

    private async Task<string?> VersionAsync(string sourceCode, string reference, CancellationToken cancellationToken) =>
        await connector.GetVersionAsync(sourceCode, reference, cancellationToken);

    private Task<WriteBackTarget?> FindTargetAsync(string sourceCode, CancellationToken cancellationToken)
    {
        var code = sourceCode.ToUpperInvariant();
        return context.WriteBackTargets.FirstOrDefaultAsync(target => target.SourceCode == code, cancellationToken);
    }

    private async Task<List<RemediationCase>> EligibleCasesAsync(
        string sourceCode,
        IReadOnlyList<Guid>? caseIds,
        int maxRecords,
        CancellationToken cancellationToken)
    {
        var query = context.Cases
            .Include(entity => entity.Proposal)
            .Include(entity => entity.History)
            .Where(entity => entity.SourceCode == sourceCode && entity.Status == CaseStatus.Approved);

        if (caseIds is { Count: > 0 })
        {
            query = query.Where(entity => caseIds.Contains(entity.Id));
        }

        return await query
            .OrderByDescending(entity => entity.PriorityScore)
            .Take(maxRecords)
            .ToListAsync(cancellationToken);
    }

    private static string RecordReference(RemediationCase entity) => entity.CaseKey;

    private static IEnumerable<(string Field, string? Before, string? After)> FieldChanges(RemediationCase entity)
    {
        if (entity.Proposal is not { } proposal)
        {
            yield break;
        }

        if (proposal.Country != entity.OriginalCountry)
        {
            yield return ("country", entity.OriginalCountry, proposal.Country);
        }

        if (proposal.TownName != entity.OriginalTownName)
        {
            yield return ("town", entity.OriginalTownName, proposal.TownName);
        }

        if (proposal.PostCode != entity.OriginalPostCode)
        {
            yield return ("postcode", entity.OriginalPostCode, proposal.PostCode);
        }

        if (proposal.StreetName != entity.OriginalStreetName)
        {
            yield return ("street", entity.OriginalStreetName, proposal.StreetName);
        }

        if (proposal.BuildingNumber != entity.OriginalBuildingNumber)
        {
            yield return ("buildingnumber", entity.OriginalBuildingNumber, proposal.BuildingNumber);
        }
    }
}
