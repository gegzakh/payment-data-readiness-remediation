using Microsoft.EntityFrameworkCore;
using PDR.Sources.Domain.Inventory;

namespace PDR.Sources.Application.Abstractions;

public interface ISourcesDbContext
{
    DbSet<SourceSystem> SourceSystems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
