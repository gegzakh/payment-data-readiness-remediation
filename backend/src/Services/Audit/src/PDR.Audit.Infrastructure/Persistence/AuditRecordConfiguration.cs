using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.Audit.Domain.Ledger;

namespace PDR.Audit.Infrastructure.Persistence;

public sealed class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        builder.ToTable("audit_records");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.Sequence).ValueGeneratedNever();
        builder.Property(record => record.Service).HasMaxLength(64).IsRequired();
        builder.Property(record => record.Action).HasMaxLength(128).IsRequired();
        builder.Property(record => record.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(record => record.EntityId).HasMaxLength(128).IsRequired();
        builder.Property(record => record.Actor).HasMaxLength(256).IsRequired();
        builder.Property(record => record.ActorId).HasMaxLength(128);
        builder.Property(record => record.CorrelationId).HasMaxLength(128);
        builder.Property(record => record.LegalEntity).HasMaxLength(64);
        // Text, not jsonb: jsonb re-formats the document it stores, and the ledger hash covers the exact
        // bytes that were appended, so the chain must round-trip verbatim.
        builder.Property(record => record.Details).HasColumnType("text");
        builder.Property(record => record.PreviousHash).HasMaxLength(64).IsRequired();
        builder.Property(record => record.Hash).HasMaxLength(64).IsRequired();
        builder.Property(record => record.Outcome).HasConversion<string>().HasMaxLength(32).IsRequired();

        // A unique sequence is what stops two concurrent appends from forking the chain.
        builder.HasIndex(record => record.Sequence).IsUnique();
        builder.HasIndex(record => record.OccurredAtUtc);
        builder.HasIndex(record => new { record.EntityType, record.EntityId });
        builder.HasIndex(record => record.CorrelationId);
    }
}
