using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Time;
using PDR.Remediation.Application.WriteBack;
using PDR.Remediation.Domain.Cases;
using PDR.Remediation.Domain.WriteBack;
using PDR.Remediation.Infrastructure.Persistence;

namespace PDR.Remediation.Infrastructure.WriteBack;

/// <summary>
/// Writes corrections into the simulated source store used by the local stack. It behaves like a real
/// target: it versions every record, refuses an update whose expected version has moved on (FR-WB-002),
/// serves read-after-write (FR-WB-005) and can restore the previous value (FR-WB-007).
/// </summary>
public sealed class SimulatedWriteBackConnector(RemediationDbContext context, IClock clock) : IWriteBackConnector
{
    public WriteBackMode Mode => WriteBackMode.Api;

    public async Task<string?> GetVersionAsync(
        string sourceCode,
        string recordReference,
        CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(sourceCode, recordReference, cancellationToken);
        return record?.Version;
    }

    public async Task<WriteBackOutcome> ApplyAsync(
        WriteBackInstruction instruction,
        CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(instruction.SourceCode, instruction.RecordReference, cancellationToken);

        if (record is null)
        {
            // A source that has never seen the record accepts it as a first write.
            context.Set<SimulatedSourceRecord>()
                .Add(new SimulatedSourceRecord(instruction.SourceCode, instruction.RecordReference, instruction.Value));

            return new WriteBackOutcome(true, null);
        }

        if (instruction.ExpectedVersion is { } expected && record.Version != expected)
        {
            return new WriteBackOutcome(false, "The record changed in the source since the correction was approved.", record.Version);
        }

        record.Write(instruction.Value, clock.UtcNow);
        return new WriteBackOutcome(true, null, record.Version);
    }

    public async Task<string?> ReadBackAsync(
        string sourceCode,
        string recordReference,
        CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(sourceCode, recordReference, cancellationToken);
        return record?.Value;
    }

    public async Task<WriteBackOutcome> RevertAsync(
        WriteBackInstruction instruction,
        CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(instruction.SourceCode, instruction.RecordReference, cancellationToken);
        if (record is null)
        {
            return new WriteBackOutcome(false, "The record no longer exists in the source.");
        }

        record.Write(instruction.Value, clock.UtcNow);
        return new WriteBackOutcome(true, null, record.Version);
    }

    private async Task<SimulatedSourceRecord?> FindAsync(
        string sourceCode,
        string recordReference,
        CancellationToken cancellationToken)
    {
        var code = sourceCode.ToUpperInvariant();

        // Records written earlier in the same run are not committed yet, so the local set is consulted
        // first; otherwise read-after-write would not see a first write.
        var pending = context.Set<SimulatedSourceRecord>().Local
            .FirstOrDefault(record => record.SourceCode == code && record.RecordReference == recordReference);

        return pending ?? await context.Set<SimulatedSourceRecord>()
            .FirstOrDefaultAsync(
                record => record.SourceCode == code && record.RecordReference == recordReference,
                cancellationToken);
    }
}
