using Microsoft.EntityFrameworkCore;
using PDR.Audit.Application.Abstractions;

namespace PDR.Audit.Infrastructure.Persistence;

/// <summary>
/// Takes a PostgreSQL transaction-scoped advisory lock, released automatically when the ambient
/// transaction commits or rolls back. Appends therefore queue instead of forking the hash chain.
/// </summary>
public sealed class PostgresAuditChainLock(AuditDbContext context) : IAuditChainLock
{
    private const long LockKey = 8246915;

    public Task AcquireAsync(CancellationToken cancellationToken = default) =>
        context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({LockKey})", cancellationToken);
}
