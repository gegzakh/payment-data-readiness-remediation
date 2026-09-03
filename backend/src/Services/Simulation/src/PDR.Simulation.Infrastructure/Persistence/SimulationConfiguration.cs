using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.BuildingBlocks.Persistence;
using PDR.Simulation.Domain.Cutover;
using PDR.Simulation.Domain.Scenarios;
using PDR.Simulation.Domain.Testing;

namespace PDR.Simulation.Infrastructure.Persistence;

public sealed class ScenarioConfiguration : IEntityTypeConfiguration<Scenario>
{
    public void Configure(EntityTypeBuilder<Scenario> builder)
    {
        builder.ToTable("scenarios");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.SchemeCodes).HasMaxLength(256);
        builder.Property(entity => entity.SourceCodes).HasMaxLength(256);
        builder.Property(entity => entity.Countries).HasMaxLength(256);
        builder.Property(entity => entity.PartyRoles).HasMaxLength(128);
        builder.Property(entity => entity.Exclusions).HasMaxLength(512);
        builder.Property(entity => entity.RulesetVersion).HasMaxLength(32);
        builder.Property(entity => entity.Description).HasMaxLength(1024);
        builder.Property(entity => entity.Mode).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(entity => entity.Code).IsUnique();

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class SimulationRunConfiguration : IEntityTypeConfiguration<SimulationRun>
{
    public void Configure(EntityTypeBuilder<SimulationRun> builder)
    {
        builder.ToTable("simulation_runs");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.ScenarioCode).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.RunKey).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.RequestedBy).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.RulesetVersion).HasMaxLength(32);
        builder.Property(entity => entity.FailureReason).HasMaxLength(512);
        builder.Property(entity => entity.Mode).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.ReadinessPercent).HasPrecision(5, 2);

        builder.HasIndex(entity => entity.ScenarioId);
        builder.HasIndex(entity => entity.RunKey);

        builder.Metadata.FindNavigation(nameof(SimulationRun.Breakdown))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(entity => entity.Breakdown)
            .WithOne()
            .HasForeignKey(row => row.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class SimulationBreakdownConfiguration : IEntityTypeConfiguration<SimulationBreakdown>
{
    public void Configure(EntityTypeBuilder<SimulationBreakdown> builder)
    {
        builder.ToTable("simulation_breakdown");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Key).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.Dimension).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(entity => new { entity.RunId, entity.Dimension });
    }
}

public sealed class TestPlanConfiguration : IEntityTypeConfiguration<TestPlan>
{
    public void Configure(EntityTypeBuilder<TestPlan> builder)
    {
        builder.ToTable("test_plans");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.Owner).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.Scope).HasMaxLength(512);
        builder.Property(entity => entity.Description).HasMaxLength(1024);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(entity => entity.Code).IsUnique();

        builder.Metadata.FindNavigation(nameof(TestPlan.Cases))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(entity => entity.Cases)
            .WithOne()
            .HasForeignKey(item => item.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class TestCaseConfiguration : IEntityTypeConfiguration<TestCase>
{
    public void Configure(EntityTypeBuilder<TestCase> builder)
    {
        builder.ToTable("test_cases");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Reference).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.ScenarioCode).HasMaxLength(32);
        builder.Property(entity => entity.SampleReference).HasMaxLength(140);
        builder.Property(entity => entity.ExpectedResult).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.ActualResult).HasMaxLength(1024);
        builder.Property(entity => entity.EvidenceReference).HasMaxLength(512);
        builder.Property(entity => entity.DefectReference).HasMaxLength(140);
        builder.Property(entity => entity.ExecutedBy).HasMaxLength(140);
        builder.Property(entity => entity.EngineOutcome).HasMaxLength(140);
        builder.Property(entity => entity.PlatformOutcome).HasMaxLength(140);
        builder.Property(entity => entity.UatExplanation).HasMaxLength(1024);
        builder.Property(entity => entity.Risk).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.UatOutcome).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(entity => new { entity.PlanId, entity.Reference }).IsUnique();
    }
}

public sealed class CutoverPlanConfiguration : IEntityTypeConfiguration<CutoverPlan>
{
    public void Configure(EntityTypeBuilder<CutoverPlan> builder)
    {
        builder.ToTable("cutover_plans");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.Owner).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.FallbackPlan).HasMaxLength(1024);
        builder.Property(entity => entity.SupportModel).HasMaxLength(1024);

        builder.HasIndex(entity => entity.Code).IsUnique();

        builder.Metadata.FindNavigation(nameof(CutoverPlan.Criteria))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(CutoverPlan.Approvals))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(entity => entity.Criteria)
            .WithOne()
            .HasForeignKey(item => item.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.Approvals)
            .WithOne()
            .HasForeignKey(item => item.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class CutoverCriterionConfiguration : IEntityTypeConfiguration<CutoverCriterion>
{
    public void Configure(EntityTypeBuilder<CutoverCriterion> builder)
    {
        builder.ToTable("cutover_criteria");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Reference).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.Owner).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.EvidenceReference).HasMaxLength(512);
        builder.Property(entity => entity.Rationale).HasMaxLength(1024);
        builder.Property(entity => entity.RecordedBy).HasMaxLength(140);
        builder.Property(entity => entity.Kind).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(entity => new { entity.PlanId, entity.Reference }).IsUnique();
    }
}

public sealed class CutoverApprovalConfiguration : IEntityTypeConfiguration<CutoverApproval>
{
    public void Configure(EntityTypeBuilder<CutoverApproval> builder)
    {
        builder.ToTable("cutover_approvals");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Role).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.Approver).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.Rationale).HasMaxLength(1024).IsRequired();
        builder.Property(entity => entity.Decision).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.RecommendationAtSignOff).HasConversion<string>().HasMaxLength(24).IsRequired();

        builder.HasIndex(entity => new { entity.PlanId, entity.Role }).IsUnique();
    }
}
