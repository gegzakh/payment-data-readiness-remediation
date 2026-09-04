using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using PDR.BuildingBlocks.Security;

namespace PDR.BuildingBlocks.UnitTests;

public sealed class PermissionAuthorizationTests
{
    [Fact]
    public async Task A_caller_holding_the_permission_is_authorized()
    {
        var context = ContextFor("remediation.approve", "remediation.approve", "rules.read");

        await new PermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Permission_matching_ignores_case_because_realm_roles_are_authored_by_hand()
    {
        var context = ContextFor("remediation.approve", "Remediation.Approve");

        await new PermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task A_neighbouring_permission_never_grants_the_requested_one()
    {
        var context = ContextFor("remediation.approve", "remediation.read", "remediation.approve.request");

        await new PermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Permission_names_become_policies_without_being_registered()
    {
        var policy = await ProviderFor(keycloakEnabled: true).GetPolicyAsync("simulation.write");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().Single().Permission.Should().Be("simulation.write");
    }

    [Fact]
    public async Task A_name_that_is_not_a_permission_is_left_to_the_default_provider()
    {
        var policy = await ProviderFor(keycloakEnabled: true).GetPolicyAsync("AdminsOnly");

        policy.Should().BeNull();
    }

    [Fact]
    public async Task Disabling_keycloak_relaxes_the_policy_instead_of_removing_the_endpoint()
    {
        var policy = await ProviderFor(keycloakEnabled: false).GetPolicyAsync("simulation.write");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().Should().BeEmpty();
    }

    private static AuthorizationHandlerContext ContextFor(string required, params string[] granted)
    {
        var identity = new ClaimsIdentity(
            granted.Select(permission => new Claim(CurrentUser.PermissionClaim, permission)),
            authenticationType: "test");

        return new AuthorizationHandlerContext(
            [new PermissionRequirement(required)],
            new ClaimsPrincipal(identity),
            resource: null);
    }

    private static PermissionPolicyProvider ProviderFor(bool keycloakEnabled) =>
        new(
            Options.Create(new AuthorizationOptions()),
            Options.Create(new KeycloakOptions { Enabled = keycloakEnabled }));
}
