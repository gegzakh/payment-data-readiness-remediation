using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.BuildingBlocks.Persistence;
using PDR.Reporting.Domain.Dashboards;

namespace PDR.Reporting.Infrastructure.Persistence;

public sealed class DashboardSnapshotConfiguration : IEntityTypeConfiguration<DashboardSnapshot>
{
    public void Configure(EntityTypeBuilder<DashboardSnapshot> builder)
    {
        builder.ToTable("dashboard_snapshots");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Audience).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.ScopeKey).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.ScopeDescription).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.SchemeCodes).HasMaxLength(512);
        builder.Property(entity => entity.SourceCodes).HasMaxLength(512);
        builder.Property(entity => entity.Countries).HasMaxLength(512);
        builder.Property(entity => entity.Exclusions).HasMaxLength(512);
        builder.Property(entity => entity.RulesetVersion).HasMaxLength(32);
        builder.Property(entity => entity.Reconciliation).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.ReconciliationNote).HasMaxLength(512);

        builder.HasIndex(entity => new { entity.Audience, entity.ScopeKey, entity.CapturedAtUtc });

        builder.Metadata.FindNavigation(nameof(DashboardSnapshot.Metrics))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(DashboardSnapshot.Breakdown))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(entity => entity.Metrics)
            .WithOne()
            .HasForeignKey(metric => metric.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.Breakdown)
            .WithOne()
            .HasForeignKey(row => row.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class MetricValueConfiguration : IEntityTypeConfiguration<MetricValue>
{
    public void Configure(EntityTypeBuilder<MetricValue> builder)
    {
        builder.ToTable("dashboard_metrics");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Key).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Label).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.Value).HasPrecision(18, 2);
        builder.Property(entity => entity.Unit).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Direction).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.DrillDimension).HasMaxLength(32);
        builder.Property(entity => entity.Text).HasMaxLength(140);

        builder.HasIndex(entity => new { entity.SnapshotId, entity.Key });
    }
}

public sealed class MetricBreakdownConfiguration : IEntityTypeConfiguration<MetricBreakdown>
{
    public void Configure(EntityTypeBuilder<MetricBreakdown> builder)
    {
        builder.ToTable("dashboard_breakdown");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Dimension).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Key).HasMaxLength(140).IsRequired();

        builder.HasIndex(entity => new { entity.SnapshotId, entity.Dimension });
    }
}
