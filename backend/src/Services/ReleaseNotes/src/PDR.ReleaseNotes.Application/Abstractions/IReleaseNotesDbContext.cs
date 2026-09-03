using Microsoft.EntityFrameworkCore;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.Application.Abstractions;

public interface IReleaseNotesDbContext
{
    DbSet<Release> Releases { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
