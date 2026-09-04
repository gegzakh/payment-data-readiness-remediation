using Microsoft.EntityFrameworkCore;
using PDR.Remediation.Domain.Campaigns;
using PDR.Remediation.Domain.Cases;
using PDR.Remediation.Domain.WriteBack;

namespace PDR.Remediation.Application.Abstractions;

public interface IRemediationDbContext
{
    DbSet<RemediationCase> Cases { get; }

    DbSet<CaseEvent> CaseEvents { get; }

    DbSet<CaseEvidence> CaseEvidence { get; }

    DbSet<Campaign> Campaigns { get; }

    DbSet<WriteBackJob> WriteBackJobs { get; }

    DbSet<WriteBackItem> WriteBackItems { get; }

    DbSet<WriteBackTarget> WriteBackTargets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
