using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.Application.Releases;

public static class ReleaseMapping
{
    public static ReleaseDto ToDto(this Release release) =>
        new(
            release.Id,
            release.Version,
            release.Title,
            release.ReleaseDate,
            release.Status,
            release.Summary,
            release.PublishedAtUtc,
            release.PublishedBy,
            release.Entries
                .GroupBy(entry => entry.Component, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new ReleaseEntryGroupDto(
                    group.Key,
                    group
                        .OrderBy(entry => entry.Type)
                        .ThenBy(entry => entry.SortOrder)
                        .Select(ToDto)
                        .ToList()))
                .ToList());

    public static ReleaseEntryDto ToDto(this ReleaseEntry entry) =>
        new(
            entry.Id,
            entry.Type,
            entry.Component,
            entry.Title,
            entry.Body,
            entry.SortOrder,
            entry.References,
            entry.IsErratum);
}
