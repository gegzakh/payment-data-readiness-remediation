using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.BuildingBlocks.Persistence;
using PDR.Rules.Domain.Reference;
using PDR.Rules.Domain.Rulesets;
using PDR.Rules.Domain.Schemes;

namespace PDR.Rules.Infrastructure.Persistence;

public sealed class SchemeConfiguration : IEntityTypeConfiguration<Scheme>
{
    public void Configure(EntityTypeBuilder<Scheme> builder)
    {
        builder.ToTable("schemes");
        builder.HasKey(scheme => scheme.Id);

        builder.Property(scheme => scheme.Code).HasMaxLength(32).IsRequired();
        builder.Property(scheme => scheme.Name).HasMaxLength(128).IsRequired();
        builder.Property(scheme => scheme.Description).HasMaxLength(2000);

        builder.HasIndex(scheme => scheme.Code).IsUnique();

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries");
        builder.HasKey(country => country.Id);

        builder.Property(country => country.Alpha2).HasMaxLength(2).IsRequired();
        builder.Property(country => country.Name).HasMaxLength(128).IsRequired();

        builder.HasIndex(country => country.Alpha2).IsUnique();

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class RulesetConfiguration : IEntityTypeConfiguration<Ruleset>
{
    public void Configure(EntityTypeBuilder<Ruleset> builder)
    {
        builder.ToTable("rulesets");
        builder.HasKey(ruleset => ruleset.Id);

        builder.Property(ruleset => ruleset.SchemeCode).HasMaxLength(32).IsRequired();
        builder.Property(ruleset => ruleset.Name).HasMaxLength(128).IsRequired();
        builder.Property(ruleset => ruleset.Description).HasMaxLength(2000);

        builder.HasIndex(ruleset => ruleset.SchemeCode).IsUnique();

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();

        builder.HasMany(ruleset => ruleset.Versions)
            .WithOne()
            .HasForeignKey(version => version.RulesetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ruleset => ruleset.Versions)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude(false);
    }
}

public sealed class RulesetVersionConfiguration : IEntityTypeConfiguration<RulesetVersion>
{
    public void Configure(EntityTypeBuilder<RulesetVersion> builder)
    {
        builder.ToTable("ruleset_versions");
        builder.HasKey(version => version.Id);

        builder.Property(version => version.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(version => version.Notes).HasMaxLength(2000);
        builder.Property(version => version.ActivatedBy).HasMaxLength(256);

        builder.HasIndex(version => new { version.RulesetId, version.VersionNumber }).IsUnique();
        builder.HasIndex(version => new { version.RulesetId, version.EffectiveFrom });

        builder.HasMany(version => version.Rules)
            .WithOne()
            .HasForeignKey(rule => rule.RulesetVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(version => version.Rules)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude(false);
    }
}

public sealed class RuleDefinitionConfiguration : IEntityTypeConfiguration<RuleDefinition>
{
    public void Configure(EntityTypeBuilder<RuleDefinition> builder)
    {
        builder.ToTable("rule_definitions");
        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Code).HasMaxLength(64).IsRequired();
        builder.Property(rule => rule.Field).HasMaxLength(64).IsRequired();
        builder.Property(rule => rule.Message).HasMaxLength(512).IsRequired();
        builder.Property(rule => rule.Parameter).HasMaxLength(1024);
        builder.Property(rule => rule.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(rule => rule.Severity).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(rule => rule.Applicability).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(rule => new { rule.RulesetVersionId, rule.Code }).IsUnique();
    }
}
