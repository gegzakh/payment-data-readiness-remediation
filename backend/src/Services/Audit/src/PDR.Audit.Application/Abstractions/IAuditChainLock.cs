namespace PDR.Audit.Application.Abstractions;

/// <summary>
/// Serialises appends to the audit hash chain across instances for the duration of the current
/// transaction. Without it two concurrent appends can link to the same predecessor and fork the chain.
/// </summary>
public interface IAuditChainLock
{
    Task AcquireAsync(CancellationToken cancellationToken = default);
}
