using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.BuildingBlocks.Domain;

namespace PDR.BuildingBlocks.Persistence;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Maps the aggregate's <c>RowVersion</c> as the optimistic concurrency token; it is incremented by
    /// <see cref="BaseDbContext"/> on every update, so lost updates surface as a 409 instead of silently winning.
    /// </summary>
    public static EntityTypeBuilder<TEntity> UseRowVersionConcurrencyToken<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IConcurrencyAware
    {
        builder.Property(entity => entity.RowVersion).IsConcurrencyToken();
        return builder;
    }

    /// <summary>Applies the same audit column conventions everywhere.</summary>
    public static EntityTypeBuilder<TEntity> ConfigureAuditColumns<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditable
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.ModifiedBy).HasMaxLength(256);
        return builder;
    }

    /// <summary>Excludes soft-deleted rows from every query unless explicitly ignored.</summary>
    public static ModelBuilder ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(ISoftDeletable).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(BuildNotDeletedFilter(entityType.ClrType));
        }

        return modelBuilder;
    }

    private static System.Linq.Expressions.LambdaExpression BuildNotDeletedFilter(Type clrType)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(clrType, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        var body = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false));
        return System.Linq.Expressions.Expression.Lambda(body, parameter);
    }
}
