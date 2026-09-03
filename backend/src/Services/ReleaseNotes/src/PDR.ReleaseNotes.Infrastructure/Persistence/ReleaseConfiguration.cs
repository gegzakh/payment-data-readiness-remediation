using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.BuildingBlocks.Persistence;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.Infrastructure.Persistence;

public sealed class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.ToTable("releases");
        builder.HasKey(release => release.Id);

        builder.Property(release => release.Version).HasMaxLength(64).IsRequired();
        builder.Property(release => release.Title).HasMaxLength(256).IsRequired();
        builder.Property(release => release.Summary).HasMaxLength(4000);
        builder.Property(release => release.PublishedBy).HasMaxLength(256);
        builder.Property(release => release.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(release => release.Version).IsUnique();
        builder.HasIndex(release => new { release.Status, release.ReleaseDate });

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();

        builder.HasMany(release => release.Entries)
            .WithOne()
            .HasForeignKey(entry => entry.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(release => release.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude(false);
    }
}

public sealed class ReleaseEntryConfiguration : IEntityTypeConfiguration<ReleaseEntry>
{
    public void Configure(EntityTypeBuilder<ReleaseEntry> builder)
    {
        builder.ToTable("release_entries");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entry => entry.Component).HasMaxLength(128).IsRequired();
        builder.Property(entry => entry.Title).HasMaxLength(256).IsRequired();
        builder.Property(entry => entry.Body).HasMaxLength(8000);
        builder.Property(entry => entry.References).HasColumnType("text[]");

        builder.HasIndex(entry => new { entry.ReleaseId, entry.SortOrder });
        builder.HasIndex(entry => entry.Component);
    }
}
