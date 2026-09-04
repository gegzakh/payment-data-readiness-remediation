using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;

namespace PDR.Rules.Domain.Reference;

/// <summary>
/// ISO 3166-1 reference data used by validation and remediation: which countries need a post code,
/// and which are in the SEPA area and therefore in scope for the structured-address cutover.
/// </summary>
public sealed class Country : AggregateRoot
{
    private Country()
    {
    }

    private Country(string alpha2, string name, bool requiresPostCode, bool isSepa)
    {
        Alpha2 = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(alpha2), 2).ToUpperInvariant();
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 128);
        RequiresPostCode = requiresPostCode;
        IsSepa = isSepa;
    }

    public string Alpha2 { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool RequiresPostCode { get; private set; }

    public bool IsSepa { get; private set; }

    public static Country Create(string alpha2, string name, bool requiresPostCode, bool isSepa) =>
        new(alpha2, name, requiresPostCode, isSepa);
}
