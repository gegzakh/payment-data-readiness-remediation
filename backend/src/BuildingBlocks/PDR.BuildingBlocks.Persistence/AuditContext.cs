using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;

namespace PDR.BuildingBlocks.Persistence;

public interface IAuditContext
{
    string Actor { get; }

    DateTimeOffset UtcNow { get; }
}

public sealed class AuditContext(ICurrentUser currentUser, IClock clock) : IAuditContext
{
    public string Actor => currentUser.IsAuthenticated ? currentUser.UserName : "system";

    public DateTimeOffset UtcNow => clock.UtcNow;
}
