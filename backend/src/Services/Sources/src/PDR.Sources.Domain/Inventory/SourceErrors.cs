using PDR.BuildingBlocks.Core.Errors;

namespace PDR.Sources.Domain.Inventory;

public static class SourceErrors
{
    public static readonly Error AlreadyExists =
        Error.Conflict("SOURCE.ALREADY_EXISTS", "A source system with this code already exists.");

    public static readonly Error DuplicateMapping =
        Error.Conflict("SOURCE.DUPLICATE_MAPPING", "This source attribute is already mapped to that element.");

    public static Error NotFound(string code) =>
        Error.NotFound("SOURCE.NOT_FOUND", $"Source system '{code}' was not found.");

    public static Error MappingNotFound(Guid id) =>
        Error.NotFound("SOURCE.MAPPING_NOT_FOUND", $"Field mapping '{id}' was not found.");

    public static Error InvalidScanCoverage() =>
        Error.Validation("SOURCE.INVALID_SCAN_COVERAGE", "Scan coverage must be between 0 and 100 percent.");
}
