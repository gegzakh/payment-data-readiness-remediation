using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using PDR.BuildingBlocks.Security;

namespace PDR.BuildingBlocks.UnitTests;

public sealed class CurrentUserTests
{
    [Fact]
    public void An_authenticated_caller_exposes_its_identity_permissions_and_scope()
    {
        var user = For(
            new Claim(ClaimTypes.NameIdentifier, "6f1a"),
            new Claim("preferred_username", "pdr-checker"),
            new Claim(CurrentUser.PermissionClaim, "remediation.approve"),
            new Claim(ClaimTypes.Role, "checker"),
            new Claim(CurrentUser.LegalEntityClaim, "LE-DE"),
            new Claim(CurrentUser.LegalEntityClaim, "LE-FR"));

        user.IsAuthenticated.Should().BeTrue();
        user.UserId.Should().Be("6f1a");
        user.UserName.Should().Be("pdr-checker");
        user.Roles.Should().BeEquivalentTo("checker");
        user.LegalEntities.Should().BeEquivalentTo("LE-DE", "LE-FR");
        user.HasPermission("REMEDIATION.APPROVE").Should().BeTrue();
        user.HasPermission("remediation.write").Should().BeFalse();
    }

    [Fact]
    public void A_request_without_a_principal_is_anonymous_rather_than_null()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var user = new CurrentUser(accessor);

        user.IsAuthenticated.Should().BeFalse();
        user.UserId.Should().Be("anonymous");
        user.UserName.Should().Be("anonymous");
        user.Permissions.Should().BeEmpty();
        user.LegalEntities.Should().BeEmpty();
    }

    [Fact]
    public void An_unscoped_caller_reports_no_legal_entities_so_callers_read_it_as_all()
    {
        var user = For(new Claim("preferred_username", "pdr-admin"));

        user.LegalEntities.Should().BeEmpty();
    }

    private static CurrentUser For(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"))
        };

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);

        return new CurrentUser(accessor);
    }
}
