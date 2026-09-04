using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.BuildingBlocks.Persistence;
using PDR.Sources.Domain.Inventory;

namespace PDR.Sources.Infrastructure.Persistence;

public sealed class SourceSystemConfiguration : IEntityTypeConfiguration<SourceSystem>
{
    public void Configure(EntityTypeBuilder<SourceSystem> builder)
    {
        builder.ToTable("source_systems");
        builder.HasKey(source => source.Id);

        builder.Property(source => source.Code).HasMaxLength(32).IsRequired();
        builder.Property(source => source.Name).HasMaxLength(128).IsRequired();
        builder.Property(source => source.OwnerName).HasMaxLength(128).IsRequired();
        builder.Property(source => source.OwnerEmail).HasMaxLength(256).IsRequired();
        builder.Property(source => source.LegalEntity).HasMaxLength(64).IsRequired();
        builder.Property(source => source.SchemeCodes).HasMaxLength(256).IsRequired();
        builder.Property(source => source.Schedule).HasMaxLength(128);
        builder.Property(source => source.RemediationOwner).HasMaxLength(128);
        builder.Property(source => source.LastAttestedBy).HasMaxLength(128);
        builder.Property(source => source.ScanCoveragePercent).HasPrecision(5, 2);
        builder.Property(source => source.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(source => source.Interface).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(source => source.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(source => source.Mapping).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(source => source.Code).IsUnique();
        builder.HasIndex(source => source.LegalEntity);

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();

        builder.HasMany(source => source.Mappings)
            .WithOne()
            .HasForeignKey(mapping => mapping.SourceSystemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(source => source.Mappings)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude(false);

        builder.HasMany(source => source.Lineage)
            .WithOne()
            .HasForeignKey(step => step.SourceSystemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(source => source.Lineage)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude(false);
    }
}

public sealed class FieldMappingConfiguration : IEntityTypeConfiguration<FieldMapping>
{
    public void Configure(EntityTypeBuilder<FieldMapping> builder)
    {
        builder.ToTable("field_mappings");
        builder.HasKey(mapping => mapping.Id);

        builder.Property(mapping => mapping.SourceAttribute).HasMaxLength(128).IsRequired();
        builder.Property(mapping => mapping.TargetElement).HasMaxLength(128).IsRequired();
        builder.Property(mapping => mapping.Transformation).HasMaxLength(512);
        builder.Property(mapping => mapping.Notes).HasMaxLength(1024);

        builder.HasIndex(mapping => new { mapping.SourceSystemId, mapping.SourceAttribute, mapping.TargetElement })
            .IsUnique();
    }
}

public sealed class LineageStepConfiguration : IEntityTypeConfiguration<LineageStep>
{
    public void Configure(EntityTypeBuilder<LineageStep> builder)
    {
        builder.ToTable("lineage_steps");
        builder.HasKey(step => step.Id);

        builder.Property(step => step.FromNode).HasMaxLength(128).IsRequired();
        builder.Property(step => step.ToNode).HasMaxLength(128).IsRequired();
        builder.Property(step => step.Channel).HasMaxLength(64);
        builder.Property(step => step.Description).HasMaxLength(512);

        builder.HasIndex(step => new { step.SourceSystemId, step.Sequence }).IsUnique();
    }
}
