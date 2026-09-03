using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Domain.WriteBack;

/// <summary>
/// One attempt to push approved corrections into an owning source system, either through its API or as
/// a controlled export. Every item keeps its before and after value so the change can be confirmed by
/// read-back and reversed later (FR-WB-003, FR-WB-005, FR-WB-007).
/// </summary>
public sealed class WriteBackJob : AggregateRoot
{
    private readonly List<WriteBackItem> _items = [];

    private WriteBackJob()
    {
    }

    private WriteBackJob(
        string targetSourceCode,
        WriteBackMode mode,
        string idempotencyKey,
        string requestedBy,
        DateTimeOffset requestedAtUtc)
    {
        TargetSourceCode = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(targetSourceCode), 32).ToUpperInvariant();
        Mode = mode;
        IdempotencyKey = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(idempotencyKey), 128);
        RequestedBy = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(requestedBy), 128);
        RequestedAtUtc = requestedAtUtc;
        Status = WriteBackStatus.Pending;
    }

    public string TargetSourceCode { get; private set; } = string.Empty;

    public WriteBackMode Mode { get; private set; }

    /// <summary>Replaying the same key must not write twice (FR-WB-003, FR-API-002).</summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    public WriteBackStatus Status { get; private set; }

    public string RequestedBy { get; private set; } = string.Empty;

    public DateTimeOffset RequestedAtUtc { get; private set; }

    public DateTimeOffset? AppliedAtUtc { get; private set; }

    public DateTimeOffset? ConfirmedAtUtc { get; private set; }

    public string? FailureSummary { get; private set; }

    /// <summary>Checksum of the exported payload, handed over with the file (FR-WB-004).</summary>
    public string? ExportChecksum { get; private set; }

    public int ItemCount => _items.Count;

    public int AppliedCount => _items.Count(item => item.Status is WriteBackItemStatus.Applied or WriteBackItemStatus.Confirmed);

    public int ConfirmedCount => _items.Count(item => item.Status == WriteBackItemStatus.Confirmed);

    public int FailedCount => _items.Count(item => item.Status == WriteBackItemStatus.Failed);

    public int StaleCount => _items.Count(item => item.Status == WriteBackItemStatus.Stale);

    public int RolledBackCount => _items.Count(item => item.Status == WriteBackItemStatus.RolledBack);

    public IReadOnlyCollection<WriteBackItem> Items => _items.AsReadOnly();

    /// <summary>Every item must end in exactly one bucket — no silent partial success (NFR-006).</summary>
    public bool CountsReconcile() =>
        Status == WriteBackStatus.Pending ||
        ItemCount == _items.Count(item => item.Status != WriteBackItemStatus.Pending);

    public static WriteBackJob Create(
        string targetSourceCode,
        WriteBackMode mode,
        string idempotencyKey,
        string requestedBy,
        DateTimeOffset requestedAtUtc) =>
        new(targetSourceCode, mode, idempotencyKey, requestedBy, requestedAtUtc);

    public WriteBackItem AddItem(
        Guid caseId,
        string recordReference,
        string? sourceVersion,
        string beforeValue,
        string afterValue)
    {
        var item = WriteBackItem.Create(Id, caseId, recordReference, sourceVersion, beforeValue, afterValue);
        _items.Add(item);
        return item;
    }

    /// <summary>Records the connector's per-item outcome and derives the job status from it.</summary>
    public void CompleteApply(DateTimeOffset appliedAtUtc, string? exportChecksum = null)
    {
        AppliedAtUtc = appliedAtUtc;
        ExportChecksum = exportChecksum;

        Status = (FailedCount + StaleCount) switch
        {
            0 => WriteBackStatus.Applied,
            var failures when failures == ItemCount => WriteBackStatus.Failed,
            _ => WriteBackStatus.PartiallyFailed
        };

        FailureSummary = FailedCount + StaleCount == 0
            ? null
            : $"{FailedCount} failed and {StaleCount} stale of {ItemCount} records.";
    }

    /// <summary>Read-after-write proved the source now holds the approved value (FR-WB-005).</summary>
    public Result Confirm(IReadOnlyCollection<Guid> confirmedItemIds, DateTimeOffset confirmedAtUtc)
    {
        if (Status is not (WriteBackStatus.Applied or WriteBackStatus.PartiallyFailed))
        {
            return Result.Failure(WriteBackErrors.NotApplied(Status));
        }

        foreach (var item in _items.Where(item => confirmedItemIds.Contains(item.Id)))
        {
            item.Confirm(confirmedAtUtc);
        }

        ConfirmedAtUtc = confirmedAtUtc;
        Status = ConfirmedCount == ItemCount ? WriteBackStatus.Confirmed : WriteBackStatus.PartiallyFailed;
        return Result.Success();
    }

    /// <summary>Reverses the applied items, restoring the value the source held before (FR-WB-007).</summary>
    public Result Rollback(string reason, DateTimeOffset atUtc)
    {
        if (Status is WriteBackStatus.Pending or WriteBackStatus.RolledBack)
        {
            return Result.Failure(WriteBackErrors.NotRollbackable(Status));
        }

        foreach (var item in _items.Where(item =>
                     item.Status is WriteBackItemStatus.Applied or WriteBackItemStatus.Confirmed))
        {
            item.Rollback(atUtc);
        }

        Status = WriteBackStatus.RolledBack;
        FailureSummary = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(reason), 512);
        return Result.Success();
    }
}

/// <summary>One record's worth of a write-back, with the values needed to confirm or reverse it.</summary>
public sealed class WriteBackItem : Entity
{
    private WriteBackItem()
    {
    }

    private WriteBackItem(
        Guid jobId,
        Guid caseId,
        string recordReference,
        string? sourceVersion,
        string beforeValue,
        string afterValue)
    {
        JobId = jobId;
        CaseId = caseId;
        RecordReference = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(recordReference), 256);
        SourceVersion = sourceVersion is null ? null : Ensure.MaxLength(sourceVersion, 128);
        BeforeValue = Ensure.MaxLength(beforeValue, 1024);
        AfterValue = Ensure.MaxLength(afterValue, 1024);
        Status = WriteBackItemStatus.Pending;
    }

    public Guid JobId { get; private set; }

    public Guid CaseId { get; private set; }

    public string RecordReference { get; private set; } = string.Empty;

    /// <summary>The source's version at proposal time; a change means the update is stale (FR-WB-002).</summary>
    public string? SourceVersion { get; private set; }

    public string BeforeValue { get; private set; } = string.Empty;

    public string AfterValue { get; private set; } = string.Empty;

    public WriteBackItemStatus Status { get; private set; }

    public string? Message { get; private set; }

    /// <summary>Correlates this write with the source system's own log (FR-WB-003).</summary>
    public string? CorrelationId { get; private set; }

    public DateTimeOffset? AppliedAtUtc { get; private set; }

    public static WriteBackItem Create(
        Guid jobId,
        Guid caseId,
        string recordReference,
        string? sourceVersion,
        string beforeValue,
        string afterValue) =>
        new(jobId, caseId, recordReference, sourceVersion, beforeValue, afterValue);

    public void Apply(string correlationId, DateTimeOffset appliedAtUtc)
    {
        Status = WriteBackItemStatus.Applied;
        CorrelationId = Ensure.MaxLength(correlationId, 64);
        AppliedAtUtc = appliedAtUtc;
        Message = null;
    }

    public void Fail(string message) 
    {
        Status = WriteBackItemStatus.Failed;
        Message = Ensure.MaxLength(message, 512);
    }

    /// <summary>The source moved on since the proposal, so the update is refused (FR-WB-002).</summary>
    public void MarkStale(string observedVersion)
    {
        Status = WriteBackItemStatus.Stale;
        Message = Ensure.MaxLength(
            $"The source version changed from '{SourceVersion}' to '{observedVersion}' since the proposal.",
            512);
    }

    public void Confirm(DateTimeOffset confirmedAtUtc)
    {
        Status = WriteBackItemStatus.Confirmed;
        AppliedAtUtc ??= confirmedAtUtc;
    }

    public void Rollback(DateTimeOffset atUtc)
    {
        Status = WriteBackItemStatus.RolledBack;
        AppliedAtUtc = atUtc;
    }
}

public static class WriteBackErrors
{
    public static Error JobNotFound(Guid id) =>
        Error.NotFound("WRITEBACK.JOB_NOT_FOUND", $"Write-back job '{id}' was not found.");

    public static Error NotApplied(WriteBackStatus status) =>
        Error.Conflict("WRITEBACK.NOT_APPLIED", $"A job in state '{status}' has nothing to confirm.");

    public static Error NotRollbackable(WriteBackStatus status) =>
        Error.Conflict("WRITEBACK.NOT_ROLLBACKABLE", $"A job in state '{status}' cannot be rolled back.");

    public static readonly Error NoEligibleCases =
        Error.Conflict("WRITEBACK.NO_ELIGIBLE_CASES", "None of the selected cases is approved and ready to write back.");

    public static Error TargetNotConfigured(string sourceCode) =>
        Error.Conflict(
            "WRITEBACK.TARGET_NOT_CONFIGURED",
            $"Source '{sourceCode}' has no authorized write-back target configured.");
}
