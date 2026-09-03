using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.BuildingBlocks.Persistence;
using PDR.Remediation.Domain.Campaigns;
using PDR.Remediation.Domain.Cases;
using PDR.Remediation.Domain.WriteBack;

namespace PDR.Remediation.Infrastructure.Persistence;

public sealed class RemediationCaseConfiguration : IEntityTypeConfiguration<RemediationCase>
{
    public void Configure(EntityTypeBuilder<RemediationCase> builder)
    {
        builder.ToTable("remediation_cases");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CaseKey).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.SourceCode).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.OwnerName).HasMaxLength(128);
        builder.Property(entity => entity.OwnerEmail).HasMaxLength(256);
        builder.Property(entity => entity.PartyName).HasMaxLength(140);
        builder.Property(entity => entity.OriginalCountry).HasMaxLength(8);
        builder.Property(entity => entity.OriginalTownName).HasMaxLength(140);
        builder.Property(entity => entity.OriginalPostCode).HasMaxLength(32);
        builder.Property(entity => entity.OriginalStreetName).HasMaxLength(140);
        builder.Property(entity => entity.OriginalBuildingNumber).HasMaxLength(32);
        builder.Property(entity => entity.OriginalAddressLines).HasMaxLength(1024);
        builder.Property(entity => entity.IssueRuleCodes).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.AffectedSchemes).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.EvidencePointer).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Queue).HasMaxLength(64);
        builder.Property(entity => entity.AssignedTo).HasMaxLength(128);
        builder.Property(entity => entity.SubmittedBy).HasMaxLength(128);
        builder.Property(entity => entity.DecidedBy).HasMaxLength(128);
        builder.Property(entity => entity.DecisionRationale).HasMaxLength(1024);
        builder.Property(entity => entity.FailureReason).HasMaxLength(512);
        builder.Property(entity => entity.PartyRole).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Priority).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(entity => entity.CaseKey).IsUnique();
        builder.HasIndex(entity => new { entity.Status, entity.Priority });
        builder.HasIndex(entity => entity.SourceCode);
        builder.HasIndex(entity => entity.CampaignId);

        builder.HasOne(entity => entity.Proposal)
            .WithOne()
            .HasForeignKey<Proposal>(proposal => proposal.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(RemediationCase.Evidence))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(RemediationCase.History))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(entity => entity.Evidence)
            .WithOne()
            .HasForeignKey(evidence => evidence.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.History)
            .WithOne()
            .HasForeignKey(item => item.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
{
    public void Configure(EntityTypeBuilder<Proposal> builder)
    {
        builder.ToTable("case_proposals");
        builder.HasKey(proposal => proposal.Id);

        builder.Property(proposal => proposal.Method).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(proposal => proposal.Country).HasMaxLength(8);
        builder.Property(proposal => proposal.TownName).HasMaxLength(140);
        builder.Property(proposal => proposal.PostCode).HasMaxLength(32);
        builder.Property(proposal => proposal.StreetName).HasMaxLength(140);
        builder.Property(proposal => proposal.BuildingNumber).HasMaxLength(32);
        builder.Property(proposal => proposal.Ambiguity).HasMaxLength(512);
        builder.Property(proposal => proposal.Alternatives).HasMaxLength(1024);
        builder.Property(proposal => proposal.Notes).HasMaxLength(1024);

        foreach (var confidence in new[]
                 {
                     nameof(Proposal.CountryConfidence), nameof(Proposal.TownConfidence),
                     nameof(Proposal.PostCodeConfidence), nameof(Proposal.StreetConfidence),
                     nameof(Proposal.BuildingNumberConfidence), nameof(Proposal.OverallConfidence)
                 })
        {
            builder.Property<decimal>(confidence).HasPrecision(5, 2);
        }

        builder.HasIndex(proposal => proposal.CaseId).IsUnique();
    }
}

public sealed class CaseEvidenceConfiguration : IEntityTypeConfiguration<CaseEvidence>
{
    public void Configure(EntityTypeBuilder<CaseEvidence> builder)
    {
        builder.ToTable("case_evidence");
        builder.HasKey(evidence => evidence.Id);

        builder.Property(evidence => evidence.Kind).HasMaxLength(64).IsRequired();
        builder.Property(evidence => evidence.Reference).HasMaxLength(512).IsRequired();
        builder.Property(evidence => evidence.Description).HasMaxLength(1024);
        builder.Property(evidence => evidence.CapturedBy).HasMaxLength(128).IsRequired();

        builder.HasIndex(evidence => evidence.CaseId);
    }
}

public sealed class CaseEventConfiguration : IEntityTypeConfiguration<CaseEvent>
{
    public void Configure(EntityTypeBuilder<CaseEvent> builder)
    {
        builder.ToTable("case_events");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Action).HasMaxLength(64).IsRequired();
        builder.Property(item => item.Actor).HasMaxLength(128).IsRequired();
        builder.Property(item => item.Rationale).HasMaxLength(1024);
        builder.Property(item => item.FromStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.ToStatus).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(item => new { item.CaseId, item.OccurredAtUtc });
    }
}

public sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("campaigns");
        builder.HasKey(campaign => campaign.Id);

        builder.Property(campaign => campaign.Code).HasMaxLength(32).IsRequired();
        builder.Property(campaign => campaign.Name).HasMaxLength(140).IsRequired();
        builder.Property(campaign => campaign.Assignee).HasMaxLength(140).IsRequired();
        builder.Property(campaign => campaign.Description).HasMaxLength(1024);
        builder.Property(campaign => campaign.Audience).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(campaign => campaign.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(campaign => campaign.Code).IsUnique();

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class WriteBackTargetConfiguration : IEntityTypeConfiguration<WriteBackTarget>
{
    public void Configure(EntityTypeBuilder<WriteBackTarget> builder)
    {
        builder.ToTable("writeback_targets");
        builder.HasKey(target => target.Id);

        builder.Property(target => target.SourceCode).HasMaxLength(32).IsRequired();
        builder.Property(target => target.WritableFields).HasMaxLength(512).IsRequired();
        builder.Property(target => target.Endpoint).HasMaxLength(512);
        builder.Property(target => target.ExportFormat).HasMaxLength(32);
        builder.Property(target => target.MaintenanceWindow).HasMaxLength(64);
        builder.Property(target => target.RollbackMethod).HasMaxLength(140).IsRequired();
        builder.Property(target => target.Mode).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(target => target.SourceCode).IsUnique();

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class WriteBackJobConfiguration : IEntityTypeConfiguration<WriteBackJob>
{
    public void Configure(EntityTypeBuilder<WriteBackJob> builder)
    {
        builder.ToTable("writeback_jobs");
        builder.HasKey(job => job.Id);

        builder.Property(job => job.TargetSourceCode).HasMaxLength(32).IsRequired();
        builder.Property(job => job.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(job => job.RequestedBy).HasMaxLength(128).IsRequired();
        builder.Property(job => job.FailureSummary).HasMaxLength(512);
        builder.Property(job => job.ExportChecksum).HasMaxLength(128);
        builder.Property(job => job.Mode).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(job => job.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(job => job.IdempotencyKey).IsUnique();
        builder.HasIndex(job => job.RequestedAtUtc);

        builder.Metadata.FindNavigation(nameof(WriteBackJob.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(job => job.Items)
            .WithOne()
            .HasForeignKey(item => item.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class WriteBackItemConfiguration : IEntityTypeConfiguration<WriteBackItem>
{
    public void Configure(EntityTypeBuilder<WriteBackItem> builder)
    {
        builder.ToTable("writeback_items");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.RecordReference).HasMaxLength(256).IsRequired();
        builder.Property(item => item.SourceVersion).HasMaxLength(128);
        builder.Property(item => item.BeforeValue).HasMaxLength(1024).IsRequired();
        builder.Property(item => item.AfterValue).HasMaxLength(1024).IsRequired();
        builder.Property(item => item.Message).HasMaxLength(512);
        builder.Property(item => item.CorrelationId).HasMaxLength(64);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(item => new { item.JobId, item.CaseId });
    }
}
