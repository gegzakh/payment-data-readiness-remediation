using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace PDR.BuildingBlocks.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    string UserId { get; }

    string UserName { get; }

    IReadOnlySet<string> Permissions { get; }

    IReadOnlySet<string> Roles { get; }

    /// <summary>Legal entities the caller is scoped to (ABAC, FR-ADM-003). Empty means "all".</summary>
    IReadOnlySet<string> LegalEntities { get; }

    bool HasPermission(string permission);
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public const string PermissionClaim = "pdr_permission";
    public const string LegalEntityClaim = "pdr_legal_entity";

    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? Principal?.FindFirstValue("sub")
                            ?? "anonymous";

    public string UserName => Principal?.FindFirstValue("preferred_username")
                              ?? Principal?.Identity?.Name
                              ?? "anonymous";

    public IReadOnlySet<string> Permissions => Collect(PermissionClaim);

    public IReadOnlySet<string> Roles => Collect(ClaimTypes.Role);

    public IReadOnlySet<string> LegalEntities => Collect(LegalEntityClaim);

    public bool HasPermission(string permission) => Permissions.Contains(permission);

    private HashSet<string> Collect(string claimType) =>
        Principal?.FindAll(claimType).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase)
        ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
