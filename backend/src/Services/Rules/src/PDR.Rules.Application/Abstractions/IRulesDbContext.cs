using Microsoft.EntityFrameworkCore;
using PDR.Rules.Domain.Reference;
using PDR.Rules.Domain.Rulesets;
using PDR.Rules.Domain.Schemes;

namespace PDR.Rules.Application.Abstractions;

public interface IRulesDbContext
{
    DbSet<Scheme> Schemes { get; }

    DbSet<Ruleset> Rulesets { get; }

    DbSet<Country> Countries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
