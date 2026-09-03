using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;

namespace PDR.ReleaseNotes.Domain.Releases;

/// <summary>
/// A single feature/change/fix line inside a release, grouped on the page by <see cref="Component"/>.
/// </summary>
public sealed class ReleaseEntry : Entity
{
    private ReleaseEntry()
    {
    }

    internal ReleaseEntry(
        Guid releaseId,
        ReleaseEntryType type,
        string component,
        string title,
        string? body,
        int sortOrder,
        IEnumerable<string>? references,
        bool isErratum)
    {
        ReleaseId = releaseId;
        Type = type;
        Component = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(component), 128);
        Title = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(title), 256);
        Body = body;
        SortOrder = sortOrder;
        References = references?.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.Ordinal).ToList()
                     ?? [];
        IsErratum = isErratum;
    }

    public Guid ReleaseId { get; private set; }

    public ReleaseEntryType Type { get; private set; }

    /// <summary>Logical part of the platform the entry belongs to (e.g. "Validation", "Remediation").</summary>
    public string Component { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    /// <summary>Markdown detail rendered on the release-notes page.</summary>
    public string? Body { get; private set; }

    public int SortOrder { get; private set; }

    public List<string> References { get; private set; } = [];

    /// <summary>Errata are the only entries that may be appended to an already published release.</summary>
    public bool IsErratum { get; private set; }

    internal void Update(
        ReleaseEntryType type,
        string component,
        string title,
        string? body,
        int sortOrder,
        IEnumerable<string>? references)
    {
        Type = type;
        Component = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(component), 128);
        Title = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(title), 256);
        Body = body;
        SortOrder = sortOrder;
        References = references?.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.Ordinal).ToList()
                     ?? [];
    }
}
