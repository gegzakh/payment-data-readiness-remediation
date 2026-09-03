using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace PDR.BuildingBlocks.Security;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var granted = context.User.FindAll(CurrentUser.PermissionClaim)
            .Any(claim => string.Equals(claim.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (granted)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Turns <c>RequireAuthorization(Permissions.X)</c> into a policy on demand, so services never
/// have to register a policy per permission.
/// </summary>
public sealed class PermissionPolicyProvider(
    IOptions<AuthorizationOptions> options,
    IOptions<KeycloakOptions> keycloakOptions) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var existing = await base.GetPolicyAsync(policyName);
        if (existing is not null)
        {
            return existing;
        }

        if (!policyName.Contains('.', StringComparison.Ordinal))
        {
            return null;
        }

        var builder = new AuthorizationPolicyBuilder();

        if (keycloakOptions.Value.Enabled)
        {
            builder.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(policyName));
        }
        else
        {
            builder.RequireAssertion(_ => true);
        }

        return builder.Build();
    }
}
