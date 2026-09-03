using PDR.BuildingBlocks.Domain;

namespace PDR.ReleaseNotes.Domain.Releases;

public sealed record ReleasePublished(
    Guid ReleaseId,
    string Version,
    DateOnly ReleaseDate,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
