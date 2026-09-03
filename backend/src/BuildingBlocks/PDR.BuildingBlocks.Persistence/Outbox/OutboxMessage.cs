using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PDR.BuildingBlocks.Persistence.Outbox;

/// <summary>
/// Transactional outbox row (NFR-006: no silent partial success). Written in the same transaction as the
/// state change and dispatched to RabbitMQ by a background publisher.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required string Type { get; init; }

    public required string Payload { get; init; }

    public required string CorrelationId { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ProcessedAtUtc { get; set; }

    public int Attempts { get; set; }

    public string? Error { get; set; }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(4000);
        builder.HasIndex(x => new { x.ProcessedAtUtc, x.OccurredAtUtc });
    }
}
