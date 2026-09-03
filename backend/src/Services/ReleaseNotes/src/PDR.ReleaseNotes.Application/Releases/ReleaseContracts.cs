using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.Application.Releases;

public sealed record ReleaseEntryDto(
    Guid Id,
    ReleaseEntryType Type,
    string Component,
    string Title,
    string? Body,
    int SortOrder,
    IReadOnlyList<string> References,
    bool IsErratum);

/// <summary>Entries of one type inside a component group, so the page can render them grouped.</summary>
public sealed record ReleaseEntryGroupDto(
    string Component,
    IReadOnlyList<ReleaseEntryDto> Entries);

public sealed record ReleaseDto(
    Guid Id,
    string Version,
    string Title,
    DateOnly ReleaseDate,
    ReleaseStatus Status,
    string? Summary,
    DateTimeOffset? PublishedAtUtc,
    string? PublishedBy,
    IReadOnlyList<ReleaseEntryGroupDto> Groups);

public sealed record ReleaseEntryInput(
    ReleaseEntryType Type,
    string Component,
    string Title,
    string? Body,
    int? SortOrder,
    IReadOnlyList<string>? References);
