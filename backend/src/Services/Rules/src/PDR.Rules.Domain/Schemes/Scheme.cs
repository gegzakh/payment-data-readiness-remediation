using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;

namespace PDR.Rules.Domain.Schemes;

/// <summary>
/// A payment scheme the platform assesses readiness for, together with the date its structured-address
/// requirement becomes mandatory (the EPC cutover for SEPA).
/// </summary>
public sealed class Scheme : AggregateRoot
{
    private Scheme()
    {
    }

    private Scheme(string code, string name, string? description, DateOnly? structuredAddressMandatoryFrom)
    {
        Code = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(code), 32).ToUpperInvariant();
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 128);
        Description = description;
        StructuredAddressMandatoryFrom = structuredAddressMandatoryFrom;
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>The cutover date after which unstructured addresses are rejected by the scheme.</summary>
    public DateOnly? StructuredAddressMandatoryFrom { get; private set; }

    public bool IsActive { get; private set; }

    public static Scheme Create(string code, string name, string? description, DateOnly? structuredAddressMandatoryFrom) =>
        new(code, name, description, structuredAddressMandatoryFrom);

    public void Update(string name, string? description, DateOnly? structuredAddressMandatoryFrom, bool isActive)
    {
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 128);
        Description = description;
        StructuredAddressMandatoryFrom = structuredAddressMandatoryFrom;
        IsActive = isActive;
    }
}
