using Microsoft.EntityFrameworkCore;
using PDR.Audit.Domain.Ledger;

namespace PDR.Audit.Application.Abstractions;

public interface IAuditDbContext
{
    DbSet<AuditRecord> AuditRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
