using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Domain;
using PDR.BuildingBlocks.Persistence.Outbox;
using PDR.BuildingBlocks.Persistence.Settings;

namespace PDR.BuildingBlocks.Persistence;

/// <summary>
/// Unit of work shared by every service: audit stamping, soft-delete filtering, optimistic concurrency,
/// and conversion of raised domain events into outbox rows within the same transaction.
/// </summary>
public abstract class BaseDbContext(DbContextOptions options, IAuditContext auditContext, ICorrelationContext correlationContext)
    : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new SystemSettingConfiguration());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAudit();
        BumpConcurrencyTokens();
        ApplySoftDelete();
        WriteOutbox();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAudit()
    {
        var now = auditContext.UtcNow;
        var actor = auditContext.Actor;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.CreatedBy = actor;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAtUtc = now;
                    entry.Entity.ModifiedBy = actor;
                    break;
            }
        }
    }

    private void BumpConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries<IConcurrencyAware>()
                     .Where(e => e.State is EntityState.Modified))
        {
            entry.Entity.RowVersion++;
        }
    }

    private void ApplySoftDelete()
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>().Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = auditContext.UtcNow;
            entry.Entity.DeletedBy = auditContext.Actor;
        }
    }

    private void WriteOutbox()
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToArray();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                OutboxMessages.Add(new OutboxMessage
                {
                    Type = domainEvent.GetType().AssemblyQualifiedName!,
                    Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    CorrelationId = correlationContext.CorrelationId,
                    OccurredAtUtc = domainEvent.OccurredAtUtc
                });
            }

            aggregate.ClearDomainEvents();
        }
    }
}
