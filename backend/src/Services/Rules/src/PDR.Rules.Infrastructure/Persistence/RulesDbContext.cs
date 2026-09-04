using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Persistence;
using PDR.Rules.Application.Abstractions;
using PDR.Rules.Domain.Reference;
using PDR.Rules.Domain.Rulesets;
using PDR.Rules.Domain.Schemes;

namespace PDR.Rules.Infrastructure.Persistence;

public sealed class RulesDbContext(
    DbContextOptions<RulesDbContext> options,
    IAuditContext auditContext,
    ICorrelationContext correlationContext)
    : BaseDbContext(options, auditContext, correlationContext), IRulesDbContext
{
    public DbSet<Scheme> Schemes => Set<Scheme>();

    public DbSet<Ruleset> Rulesets => Set<Ruleset>();

    public DbSet<Country> Countries => Set<Country>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RulesDbContext).Assembly);
    }
}
