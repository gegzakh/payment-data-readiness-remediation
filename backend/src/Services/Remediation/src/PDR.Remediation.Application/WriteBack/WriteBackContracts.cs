using PDR.Remediation.Domain.Cases;
using PDR.Remediation.Domain.WriteBack;

namespace PDR.Remediation.Application.WriteBack;

public sealed record WriteBackTargetDto(
    Guid Id,
    string SourceCode,
    WriteBackMode Mode,
    string WritableFields,
    string? Endpoint,
    string? ExportFormat,
    string? MaintenanceWindow,
    int MaxRecordsPerRun,
    bool RequiresApproval,
    string RollbackMethod,
    bool IsEnabled);

public sealed record WriteBackJobDto(
    Guid Id,
    string TargetSourceCode,
    WriteBackMode Mode,
    WriteBackStatus Status,
    string IdempotencyKey,
    string RequestedBy,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? AppliedAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    string? FailureSummary,
    string? ExportChecksum,
    int ItemCount,
    int AppliedCount,
    int ConfirmedCount,
    int FailedCount,
    int StaleCount,
    int RolledBackCount,
    bool CountsReconcile,
    IReadOnlyList<WriteBackItemDto> Items);

public sealed record WriteBackItemDto(
    Guid Id,
    Guid CaseId,
    string RecordReference,
    string? SourceVersion,
    string BeforeValue,
    string AfterValue,
    WriteBackItemStatus Status,
    string? Message,
    string? CorrelationId,
    DateTimeOffset? AppliedAtUtc);

/// <summary>What a write-back would do, before it does it (FR-WB-002).</summary>
public sealed record WriteBackPreviewDto(
    string TargetSourceCode,
    WriteBackMode Mode,
    string? MaintenanceWindow,
    int MaxRecordsPerRun,
    string RollbackMethod,
    int EligibleCases,
    int RecordsToWrite,
    IReadOnlyList<WriteBackChangeDto> Changes,
    IReadOnlyList<string> Blockers);

public sealed record WriteBackChangeDto(
    Guid CaseId,
    string RecordReference,
    string Field,
    string? BeforeValue,
    string? AfterValue);

/// <summary>The reconciliation of one job: what was written versus what the source confirms (FR-WB-005).</summary>
public sealed record WriteBackReconciliationDto(
    Guid JobId,
    int Requested,
    int Applied,
    int Confirmed,
    int Failed,
    int Stale,
    int RolledBack,
    bool Balanced,
    IReadOnlyList<string> Discrepancies);

/// <summary>
/// The channel through which corrections reach a source system. The simulated connector is the local
/// implementation; a real deployment registers one per source.
/// </summary>
public interface IWriteBackConnector
{
    WriteBackMode Mode { get; }

    /// <summary>The version the source currently holds, used to refuse stale updates (FR-WB-002).</summary>
    Task<string?> GetVersionAsync(string sourceCode, string recordReference, CancellationToken cancellationToken = default);

    Task<WriteBackOutcome> ApplyAsync(WriteBackInstruction instruction, CancellationToken cancellationToken = default);

    /// <summary>Reads the record back to prove the correction landed (FR-WB-005).</summary>
    Task<string?> ReadBackAsync(string sourceCode, string recordReference, CancellationToken cancellationToken = default);

    /// <summary>Restores the value the source held before the correction (FR-WB-007).</summary>
    Task<WriteBackOutcome> RevertAsync(WriteBackInstruction instruction, CancellationToken cancellationToken = default);
}

public sealed record WriteBackInstruction(
    string SourceCode,
    string RecordReference,
    string? ExpectedVersion,
    string Value,
    string CorrelationId);

public sealed record WriteBackOutcome(bool Succeeded, string? Message, string? ObservedVersion = null);

public static class WriteBackDefaults
{
    public const int MaxRecordsPerRun = 500;
    public const bool ReadBackAfterWrite = true;
}

public static class WriteBackSettingKeys
{
    public const string ReadBackAfterWrite = "Remediation:WriteBack:ReadBackAfterWrite";
    public const string MaxRecordsPerRun = "Remediation:WriteBack:MaxRecordsPerRun";
}

public static class WriteBackMapper
{
    public static WriteBackTargetDto ToDto(this WriteBackTarget target) =>
        new(
            target.Id,
            target.SourceCode,
            target.Mode,
            target.WritableFields,
            target.Endpoint,
            target.ExportFormat,
            target.MaintenanceWindow,
            target.MaxRecordsPerRun,
            target.RequiresApproval,
            target.RollbackMethod,
            target.IsEnabled);

    public static WriteBackJobDto ToDto(this WriteBackJob job) =>
        new(
            job.Id,
            job.TargetSourceCode,
            job.Mode,
            job.Status,
            job.IdempotencyKey,
            job.RequestedBy,
            job.RequestedAtUtc,
            job.AppliedAtUtc,
            job.ConfirmedAtUtc,
            job.FailureSummary,
            job.ExportChecksum,
            job.ItemCount,
            job.AppliedCount,
            job.ConfirmedCount,
            job.FailedCount,
            job.StaleCount,
            job.RolledBackCount,
            job.CountsReconcile(),
            [.. job.Items.Select(item => new WriteBackItemDto(
                item.Id,
                item.CaseId,
                item.RecordReference,
                item.SourceVersion,
                item.BeforeValue,
                item.AfterValue,
                item.Status,
                item.Message,
                item.CorrelationId,
                item.AppliedAtUtc))]);

    /// <summary>The canonical shape written to a source, so before and after are comparable.</summary>
    public static string Render(
        string? country,
        string? town,
        string? postCode,
        string? street,
        string? buildingNumber) =>
        string.Join(
            '|',
            $"country={country}",
            $"town={town}",
            $"postCode={postCode}",
            $"street={street}",
            $"buildingNumber={buildingNumber}");

    public static string RenderOriginal(RemediationCase entity) =>
        Render(
            entity.OriginalCountry,
            entity.OriginalTownName,
            entity.OriginalPostCode,
            entity.OriginalStreetName,
            entity.OriginalBuildingNumber);

    public static string RenderProposed(RemediationCase entity) =>
        entity.Proposal is null
            ? string.Empty
            : Render(
                entity.Proposal.Country,
                entity.Proposal.TownName,
                entity.Proposal.PostCode,
                entity.Proposal.StreetName,
                entity.Proposal.BuildingNumber);
}
