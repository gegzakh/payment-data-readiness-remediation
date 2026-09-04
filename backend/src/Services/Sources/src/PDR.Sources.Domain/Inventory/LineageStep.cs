using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;

namespace PDR.Sources.Domain.Inventory;

/// <summary>
/// One hop on the path from the authoritative party/address record to the submitted payment message
/// (FR-SRC-002), e.g. <c>Customer master → Channel template → Payment hub → pacs.008</c>.
/// </summary>
public sealed class LineageStep : Entity
{
    private LineageStep()
    {
    }

    private LineageStep(int sequence, string fromNode, string toNode, string? channel, string? description)
    {
        Sequence = sequence;
        FromNode = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(fromNode), 128);
        ToNode = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(toNode), 128);
        Channel = channel;
        Description = description;
    }

    public Guid SourceSystemId { get; private set; }

    public int Sequence { get; private set; }

    public string FromNode { get; private set; } = string.Empty;

    public string ToNode { get; private set; } = string.Empty;

    public string? Channel { get; private set; }

    public string? Description { get; private set; }

    public static LineageStep Create(int sequence, string fromNode, string toNode, string? channel, string? description) =>
        new(sequence, fromNode, toNode, channel, description);
}
