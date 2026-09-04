using Microsoft.EntityFrameworkCore;
using PDR.Simulation.Domain.Cutover;
using PDR.Simulation.Domain.Scenarios;
using PDR.Simulation.Domain.Testing;

namespace PDR.Simulation.Application.Abstractions;

public interface ISimulationDbContext
{
    DbSet<Scenario> Scenarios { get; }

    DbSet<SimulationRun> Runs { get; }

    DbSet<SimulationBreakdown> Breakdown { get; }

    DbSet<TestPlan> TestPlans { get; }

    DbSet<TestCase> TestCases { get; }

    DbSet<CutoverPlan> CutoverPlans { get; }

    DbSet<CutoverCriterion> CutoverCriteria { get; }

    DbSet<CutoverApproval> CutoverApprovals { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
