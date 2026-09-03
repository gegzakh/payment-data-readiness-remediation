using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.BuildingBlocks.Persistence;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Infrastructure.Persistence;

public sealed class ValidationRunConfiguration : IEntityTypeConfiguration<ValidationRun>
{
    public void Configure(EntityTypeBuilder<ValidationRun> builder)
    {
        builder.ToTable("validation_runs");
        builder.HasKey(run => run.Id);

        builder.Property(run => run.SourceCode).HasMaxLength(32).IsRequired();
        builder.Property(run => run.SchemeCode).HasMaxLength(32).IsRequired();
        builder.Property(run => run.ErrorSummary).HasMaxLength(1024);
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(run => run.BatchId);
        builder.HasIndex(run => run.StartedAtUtc);

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class AddressAssessmentConfiguration : IEntityTypeConfiguration<AddressAssessment>
{
    public void Configure(EntityTypeBuilder<AddressAssessment> builder)
    {
        builder.ToTable("address_assessments");
        builder.HasKey(assessment => assessment.Id);

        builder.Property(assessment => assessment.SourceCode).HasMaxLength(32).IsRequired();
        builder.Property(assessment => assessment.MessageId).HasMaxLength(64);
        builder.Property(assessment => assessment.EndToEndId).HasMaxLength(64);
        builder.Property(assessment => assessment.PartyName).HasMaxLength(140);
        builder.Property(assessment => assessment.Country).HasMaxLength(8);
        builder.Property(assessment => assessment.TownName).HasMaxLength(140);
        builder.Property(assessment => assessment.PostCode).HasMaxLength(32);
        builder.Property(assessment => assessment.StreetName).HasMaxLength(140);
        builder.Property(assessment => assessment.BuildingNumber).HasMaxLength(32);
        builder.Property(assessment => assessment.AddressLines).HasMaxLength(1024);
        builder.Property(assessment => assessment.SchemeCode).HasMaxLength(32);
        builder.Property(assessment => assessment.EvidencePointer).HasMaxLength(128).IsRequired();
        builder.Property(assessment => assessment.PartyRole).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(assessment => assessment.Classification).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(assessment => assessment.CurrentOutcome).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(assessment => assessment.FutureOutcome).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(assessment => new { assessment.RunId, assessment.Sequence });
        builder.HasIndex(assessment => assessment.BatchId);

        builder.HasOne<ValidationRun>()
            .WithMany()
            .HasForeignKey(assessment => assessment.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(AddressAssessment.Issues))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(assessment => assessment.Issues)
            .WithOne()
            .HasForeignKey(issue => issue.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ValidationIssueConfiguration : IEntityTypeConfiguration<ValidationIssue>
{
    public void Configure(EntityTypeBuilder<ValidationIssue> builder)
    {
        builder.ToTable("validation_issues");
        builder.HasKey(issue => issue.Id);

        builder.Property(issue => issue.RuleCode).HasMaxLength(64).IsRequired();
        builder.Property(issue => issue.Field).HasMaxLength(64).IsRequired();
        builder.Property(issue => issue.Message).HasMaxLength(512).IsRequired();
        builder.Property(issue => issue.Expected).HasMaxLength(256);
        builder.Property(issue => issue.Actual).HasMaxLength(256);
        builder.Property(issue => issue.Mode).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(issue => issue.Severity).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(issue => new { issue.RuleCode, issue.Mode });
    }
}
