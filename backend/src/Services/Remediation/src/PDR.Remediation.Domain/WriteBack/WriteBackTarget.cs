using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Domain.WriteBack;

/// <summary>
/// The authorized channel for correcting one source: which fields may be written, how, when and how a
/// change is reversed (FR-WB-001). Nothing is written to a source without one of these.
/// </summary>
public sealed class WriteBackTarget : AggregateRoot
{
    private WriteBackTarget()
    {
    }

    private WriteBackTarget(
        string sourceCode,
        WriteBackMode mode,
        string writableFields,
        string? endpoint,
        string? exportFormat,
        string? maintenanceWindow,
        int maxRecordsPerRun,
        bool requiresApproval,
        string rollbackMethod)
    {
        SourceCode = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(sourceCode), 32).ToUpperInvariant();
        Mode = mode;
        WritableFields = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(writableFields), 512).ToLowerInvariant();
        Endpoint = endpoint is null ? null : Ensure.MaxLength(endpoint, 512);
        ExportFormat = exportFormat is null ? null : Ensure.MaxLength(exportFormat, 32);
        MaintenanceWindow = maintenanceWindow is null ? null : Ensure.MaxLength(maintenanceWindow, 64);
        MaxRecordsPerRun = Math.Clamp(maxRecordsPerRun, 1, 100_000);
        RequiresApproval = requiresApproval;
        RollbackMethod = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(rollbackMethod), 140);
        IsEnabled = true;
    }

    public string SourceCode { get; private set; } = string.Empty;

    public WriteBackMode Mode { get; private set; }

    /// <summary>Comma-separated field names the source accepts; anything else is refused.</summary>
    public string WritableFields { get; private set; } = string.Empty;

    public string? Endpoint { get; private set; }

    public string? ExportFormat { get; private set; }

    /// <summary>Free-text window (for example "Sat 22:00-02:00 UTC") shown before a run (FR-WB-001).</summary>
    public string? MaintenanceWindow { get; private set; }

    /// <summary>Rate limit expressed as the largest batch the source will take in one run.</summary>
    public int MaxRecordsPerRun { get; private set; }

    public bool RequiresApproval { get; private set; }

    public string RollbackMethod { get; private set; } = string.Empty;

    public bool IsEnabled { get; private set; }

    public static WriteBackTarget Create(
        string sourceCode,
        WriteBackMode mode,
        string writableFields,
        string? endpoint,
        string? exportFormat,
        string? maintenanceWindow,
        int maxRecordsPerRun,
        bool requiresApproval,
        string rollbackMethod) =>
        new(
            sourceCode,
            mode,
            writableFields,
            endpoint,
            exportFormat,
            maintenanceWindow,
            maxRecordsPerRun,
            requiresApproval,
            rollbackMethod);

    public bool Allows(string field) =>
        WritableFields
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(field.ToLowerInvariant());

    public void Disable() => IsEnabled = false;

    public void Enable() => IsEnabled = true;
}
