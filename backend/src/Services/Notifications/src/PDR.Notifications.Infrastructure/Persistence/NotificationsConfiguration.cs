using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.BuildingBlocks.Persistence;
using PDR.Notifications.Domain.Schedules;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.Infrastructure.Persistence;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.EventPattern).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Channel).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Target).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.SchemeCodes).HasMaxLength(512);
        builder.Property(entity => entity.SourceCodes).HasMaxLength(512);
        builder.Property(entity => entity.MinimumSeverity).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.SigningSecret).HasMaxLength(256);
        builder.Property(entity => entity.Owner).HasMaxLength(140).IsRequired();

        builder.HasIndex(entity => entity.Code).IsUnique();

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.EventType).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Severity).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Subject).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Payload).IsRequired();
        builder.Property(entity => entity.SchemeCode).HasMaxLength(32);
        builder.Property(entity => entity.SourceCode).HasMaxLength(64);
        builder.Property(entity => entity.PublishedBy).HasMaxLength(140).IsRequired();

        // The unique key is what makes a repeated publish a no-op rather than a second fan-out.
        builder.HasIndex(entity => entity.IdempotencyKey).IsUnique();
        builder.HasIndex(entity => new { entity.EventType, entity.OccurredAtUtc });

        builder.Metadata.FindNavigation(nameof(Notification.Deliveries))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(entity => entity.Deliveries)
            .WithOne()
            .HasForeignKey(delivery => delivery.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}

public sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("deliveries");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.SubscriptionCode).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Channel).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Target).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.LastError).HasMaxLength(512);
        builder.Property(entity => entity.Signature).HasMaxLength(256);

        // The dispatcher polls on exactly this predicate.
        builder.HasIndex(entity => new { entity.Status, entity.NextAttemptAtUtc });
        builder.HasIndex(entity => entity.SubscriptionCode);
    }
}

public sealed class ScheduledReportConfiguration : IEntityTypeConfiguration<ScheduledReport>
{
    public void Configure(EntityTypeBuilder<ScheduledReport> builder)
    {
        builder.ToTable("scheduled_reports");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(140).IsRequired();
        builder.Property(entity => entity.Audience).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.SchemeCodes).HasMaxLength(512);
        builder.Property(entity => entity.SourceCodes).HasMaxLength(512);
        builder.Property(entity => entity.Frequency).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Recipients).HasMaxLength(1024).IsRequired();
        builder.Property(entity => entity.Owner).HasMaxLength(140).IsRequired();

        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => entity.NextRunAtUtc);

        builder.ConfigureAuditColumns();
        builder.UseRowVersionConcurrencyToken();
    }
}
