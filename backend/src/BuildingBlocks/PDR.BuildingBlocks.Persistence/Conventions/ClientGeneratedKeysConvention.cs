using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using PDR.BuildingBlocks.Domain;

namespace PDR.BuildingBlocks.Persistence.Conventions;

/// <summary>
/// Entities assign their own <see cref="Entity.Id"/> in the constructor. Without this, EF treats the key as
/// store-generated and a child discovered through a navigation with a key already set is tracked as
/// <c>Modified</c> instead of <c>Added</c>, which turns an insert into an update of a non-existent row.
/// </summary>
public sealed class ClientGeneratedKeysConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes()
                     .Where(type => typeof(Entity).IsAssignableFrom(type.ClrType)))
        {
            var key = entityType.FindPrimaryKey();
            if (key is null || key.Properties.Count != 1)
            {
                continue;
            }

            var property = key.Properties[0];
            if (property.ClrType == typeof(Guid))
            {
                property.Builder.ValueGenerated(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never);
            }
        }
    }
}
