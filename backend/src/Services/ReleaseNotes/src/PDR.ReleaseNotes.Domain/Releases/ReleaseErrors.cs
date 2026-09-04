using PDR.BuildingBlocks.Core.Errors;

namespace PDR.ReleaseNotes.Domain.Releases;

public static class ReleaseErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("RELEASE.NOT_FOUND", $"Release '{id}' was not found.");

    public static Error EntryNotFound(Guid id) =>
        Error.NotFound("RELEASE.ENTRY_NOT_FOUND", $"Release entry '{id}' was not found.");

    public static readonly Error VersionAlreadyExists =
        Error.Conflict("RELEASE.VERSION_EXISTS", "A release with this version already exists.");

    public static readonly Error AlreadyPublished =
        Error.Conflict("RELEASE.ALREADY_PUBLISHED", "The release is already published.");

    public static readonly Error PublishedIsImmutable =
        Error.Conflict(
            "RELEASE.PUBLISHED_IMMUTABLE",
            "A published release cannot be modified; append an erratum entry instead.");

    public static readonly Error NoEntries =
        Error.Unprocessable("RELEASE.NO_ENTRIES", "A release must contain at least one entry before publication.");

    public static readonly Error ErrataRequirePublished =
        Error.Conflict("RELEASE.ERRATUM_REQUIRES_PUBLISHED", "Errata can only be appended to a published release.");
}
