using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;

namespace PDR.ReleaseNotes.Domain.Releases;

/// <summary>
/// Aggregate root for a release and its entries. Drafts are freely editable; once published the release
/// becomes immutable apart from appended errata, so the published history stays evidential.
/// </summary>
public sealed class Release : AggregateRoot
{
    private readonly List<ReleaseEntry> _entries = [];

    private Release()
    {
    }

    private Release(string version, string title, DateOnly releaseDate, string? summary)
    {
        Version = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(version), 64);
        Title = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(title), 256);
        ReleaseDate = releaseDate;
        Summary = summary;
        Status = ReleaseStatus.Draft;
    }

    public string Version { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public DateOnly ReleaseDate { get; private set; }

    public ReleaseStatus Status { get; private set; }

    public string? Summary { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public string? PublishedBy { get; private set; }

    public IReadOnlyList<ReleaseEntry> Entries => _entries;

    public static Release CreateDraft(string version, string title, DateOnly releaseDate, string? summary) =>
        new(version, title, releaseDate, summary);

    public Result UpdateDetails(string version, string title, DateOnly releaseDate, string? summary)
    {
        if (Status == ReleaseStatus.Published)
        {
            return Result.Failure(ReleaseErrors.PublishedIsImmutable);
        }

        Version = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(version), 64);
        Title = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(title), 256);
        ReleaseDate = releaseDate;
        Summary = summary;
        return Result.Success();
    }

    public Result<ReleaseEntry> AddEntry(
        ReleaseEntryType type,
        string component,
        string title,
        string? body,
        int? sortOrder,
        IEnumerable<string>? references)
    {
        if (Status == ReleaseStatus.Published)
        {
            return Result.Failure<ReleaseEntry>(ReleaseErrors.PublishedIsImmutable);
        }

        var entry = new ReleaseEntry(
            Id,
            type,
            component,
            title,
            body,
            sortOrder ?? NextSortOrder(),
            references,
            isErratum: false);

        _entries.Add(entry);
        return entry;
    }

    /// <summary>The one mutation allowed after publication: a corrective note appended to the record.</summary>
    public Result<ReleaseEntry> AddErratum(
        string component,
        string title,
        string? body,
        IEnumerable<string>? references)
    {
        if (Status != ReleaseStatus.Published)
        {
            return Result.Failure<ReleaseEntry>(ReleaseErrors.ErrataRequirePublished);
        }

        var entry = new ReleaseEntry(
            Id,
            ReleaseEntryType.Change,
            component,
            title,
            body,
            NextSortOrder(),
            references,
            isErratum: true);

        _entries.Add(entry);
        return entry;
    }

    public Result UpdateEntry(
        Guid entryId,
        ReleaseEntryType type,
        string component,
        string title,
        string? body,
        int sortOrder,
        IEnumerable<string>? references)
    {
        if (Status == ReleaseStatus.Published)
        {
            return Result.Failure(ReleaseErrors.PublishedIsImmutable);
        }

        var entry = _entries.Find(e => e.Id == entryId);
        if (entry is null)
        {
            return Result.Failure(ReleaseErrors.EntryNotFound(entryId));
        }

        entry.Update(type, component, title, body, sortOrder, references);
        return Result.Success();
    }

    public Result RemoveEntry(Guid entryId)
    {
        if (Status == ReleaseStatus.Published)
        {
            return Result.Failure(ReleaseErrors.PublishedIsImmutable);
        }

        var entry = _entries.Find(e => e.Id == entryId);
        if (entry is null)
        {
            return Result.Failure(ReleaseErrors.EntryNotFound(entryId));
        }

        _entries.Remove(entry);
        return Result.Success();
    }

    public Result Publish(string publishedBy, DateTimeOffset publishedAtUtc)
    {
        if (Status == ReleaseStatus.Published)
        {
            return Result.Failure(ReleaseErrors.AlreadyPublished);
        }

        if (_entries.Count == 0)
        {
            return Result.Failure(ReleaseErrors.NoEntries);
        }

        Status = ReleaseStatus.Published;
        PublishedBy = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(publishedBy), 256);
        PublishedAtUtc = publishedAtUtc;

        Raise(new ReleasePublished(Id, Version, ReleaseDate, publishedAtUtc));
        return Result.Success();
    }

    private int NextSortOrder() => _entries.Count == 0 ? 0 : _entries.Max(e => e.SortOrder) + 1;
}
