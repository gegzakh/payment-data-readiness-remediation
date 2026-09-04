using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.BuildingBlocks.Persistence;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Infrastructure.Persistence;

public sealed class IngestionBatchConfiguration : IEntityTypeConfiguration<IngestionBatch>
{
    public void Configure(EntityTypeBuilder<IngestionBatch> builder)
    {
        builder.ToTable("ingestion_batches");
        builder.HasKey(batch => batch.Id);

        builder.Property(batch => batch.SourceCode).HasMaxLength(32).IsRequired();
        builder.Property(batch => batch.FileName).HasMaxLength(256).IsRequired();
        builder.Property(batch => batch.Checksum).HasMaxLength(64).IsRequired();
        builder.Property(batch => batch.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(batch => batch.ParserVersion).HasMaxLength(32).IsRequired();
        builder.Property(batch => batch.SubmittedBy).HasMaxLength(128).IsRequired();
        builder.Property(batch => batch.QuarantineReason).HasMaxLength(512);
        builder.Property(batch => batch.ErrorSummary).HasMaxLength(1024);
        builder.Property(batch => batch.Format).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(batch => batch.Channel).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(batch => batch.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(batch => batch.IdempotencyKey).IsUnique();
        builder.HasIndex(batch => new { batch.SourceCode, batch.Checksum });
        builder.HasIndex(batch => batch.ReceivedAtUtc);

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class PartyAddressRecordConfiguration : IEntityTypeConfiguration<PartyAddressRecord>
{
    public void Configure(EntityTypeBuilder<PartyAddressRecord> builder)
    {
        builder.ToTable("party_address_records");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.MessageId).HasMaxLength(64);
        builder.Property(record => record.EndToEndId).HasMaxLength(64);
        builder.Property(record => record.PartyName).HasMaxLength(140);
        builder.Property(record => record.Country).HasMaxLength(8);
        builder.Property(record => record.TownName).HasMaxLength(140);
        builder.Property(record => record.PostCode).HasMaxLength(32);
        builder.Property(record => record.StreetName).HasMaxLength(140);
        builder.Property(record => record.BuildingNumber).HasMaxLength(32);
        builder.Property(record => record.AddressLines).HasMaxLength(1024);
        builder.Property(record => record.SchemeCode).HasMaxLength(32);
        builder.Property(record => record.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(record => record.PartyRole).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(record => new { record.BatchId, record.Sequence }).IsUnique();
        builder.HasIndex(record => record.ContentHash);

        builder.HasOne<IngestionBatch>()
            .WithMany()
            .HasForeignKey(record => record.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BatchPayloadConfiguration : IEntityTypeConfiguration<BatchPayload>
{
    public void Configure(EntityTypeBuilder<BatchPayload> builder)
    {
        builder.ToTable("batch_payloads");
        builder.HasKey(payload => payload.Id);

        builder.Property(payload => payload.Content).IsRequired();
        builder.HasIndex(payload => payload.BatchId).IsUnique();

        builder.HasOne<IngestionBatch>()
            .WithMany()
            .HasForeignKey(payload => payload.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
