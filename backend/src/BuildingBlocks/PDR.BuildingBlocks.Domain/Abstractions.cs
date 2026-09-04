namespace PDR.BuildingBlocks.Domain;

public interface IDomainEvent
{
    Guid EventId => Guid.CreateVersion7();

    DateTimeOffset OccurredAtUtc { get; }
}

public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; set; }

    string CreatedBy { get; set; }

    DateTimeOffset? ModifiedAtUtc { get; set; }

    string? ModifiedBy { get; set; }
}

public interface IConcurrencyAware
{
    uint RowVersion { get; set; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTimeOffset? DeletedAtUtc { get; set; }

    string? DeletedBy { get; set; }
}

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other) =>
        other is not null && other.GetType() == GetType() &&
        GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }
}
