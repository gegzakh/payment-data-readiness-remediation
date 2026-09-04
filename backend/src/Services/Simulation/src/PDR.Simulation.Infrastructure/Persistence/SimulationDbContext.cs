using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Persistence;
using PDR.Simulation.Application.Abstractions;
using PDR.Simulation.Domain.Cutover;
using PDR.Simulation.Domain.Scenarios;
using PDR.Simulation.Domain.Testing;

namespace PDR.Simulation.Infrastructure.Persistence;

public sealed class SimulationDbContext(
    DbContextOptions<SimulationDbContext> options,
    IAuditContext auditContext,
    ICorrelationContext correlationContext)
    : BaseDbContext(options, auditContext, correlationContext), ISimulationDbContext
{
    public DbSet<Scenario> Scenarios => Set<Scenario>();

    public DbSet<SimulationRun> Runs => Set<SimulationRun>();

    public DbSet<SimulationBreakdown> Breakdown => Set<SimulationBreakdown>();

    public DbSet<TestPlan> TestPlans => Set<TestPlan>();

    public DbSet<TestCase> TestCases => Set<TestCase>();

    public DbSet<CutoverPlan> CutoverPlans => Set<CutoverPlan>();

    public DbSet<CutoverCriterion> CutoverCriteria => Set<CutoverCriterion>();

    public DbSet<CutoverApproval> CutoverApprovals => Set<CutoverApproval>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimulationDbContext).Assembly);
    }
}
