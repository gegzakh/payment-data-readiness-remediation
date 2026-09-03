using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;

namespace PDR.Sources.Domain.Inventory;

/// <summary>
/// Field-level mapping from a source attribute to the ISO 20022 address element it ends up in
/// (FR-SRC-003). The mapping is what makes a validation finding traceable back to the system that
/// must be fixed.
/// </summary>
public sealed class FieldMapping : Entity
{
    private FieldMapping()
    {
    }

    private FieldMapping(
        string sourceAttribute,
        string targetElement,
        string? transformation,
        bool isAuthoritative,
        string? notes)
    {
        SourceAttribute = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(sourceAttribute), 128);
        TargetElement = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(targetElement), 128);
        Transformation = transformation;
        IsAuthoritative = isAuthoritative;
        Notes = notes;
    }

    public Guid SourceSystemId { get; private set; }

    /// <summary>Attribute name in the source system, e.g. <c>CUSTOMER.ADDR_LINE_1</c>.</summary>
    public string SourceAttribute { get; private set; } = string.Empty;

    /// <summary>ISO 20022 element it feeds, e.g. <c>PstlAdr/StrtNm</c>.</summary>
    public string TargetElement { get; private set; } = string.Empty;

    public string? Transformation { get; private set; }

    /// <summary>True when this source is the book of record for the attribute (FR-SRC-001).</summary>
    public bool IsAuthoritative { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset? LastReviewedAtUtc { get; private set; }

    public static FieldMapping Create(
        string sourceAttribute,
        string targetElement,
        string? transformation,
        bool isAuthoritative,
        string? notes) =>
        new(sourceAttribute, targetElement, transformation, isAuthoritative, notes);

    public void MarkReviewed(DateTimeOffset reviewedAtUtc) => LastReviewedAtUtc = reviewedAtUtc;
}
