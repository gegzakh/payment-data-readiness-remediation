using PDR.BuildingBlocks.Core.Errors;

namespace PDR.Audit.Domain.Ledger;

public static class AuditErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("AUDIT.NOT_FOUND", $"Audit record '{id}' was not found.");
}
