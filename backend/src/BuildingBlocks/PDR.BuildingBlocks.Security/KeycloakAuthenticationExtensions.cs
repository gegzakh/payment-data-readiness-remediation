using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace PDR.BuildingBlocks.Security;

public sealed class KeycloakOptions
{
    public const string SectionName = "Authentication:Keycloak";

    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Discovery document to read instead of the one derived from <see cref="Authority"/>, for deployments where
    /// the browser and the services reach Keycloak on different hosts (container network versus published port).
    /// </summary>
    public string MetadataAddress { get; set; } = string.Empty;

    public string Audience { get; set; } = "pdr-api";

    /// <summary>Keycloak client whose roles are mapped to PDR permissions.</summary>
    public string ResourceClient { get; set; } = "pdr-api";

    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Development escape hatch: when false, endpoints still declare policies but tokens are not required.</summary>
    public bool Enabled { get; set; } = true;
}

public static class KeycloakAuthenticationExtensions
{
    /// <summary>
    /// Configures the service as a pure OAuth2 resource server in front of Keycloak: realm and client
    /// roles are flattened into <see cref="CurrentUser.PermissionClaim"/> claims so that authorization
    /// policies stay identical across every microservice.
    /// </summary>
    public static IServiceCollection AddPdrAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>() ?? new KeycloakOptions();
        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = options.Authority;

                if (!string.IsNullOrWhiteSpace(options.MetadataAddress))
                {
                    jwt.MetadataAddress = options.MetadataAddress;
                }

                jwt.Audience = options.Audience;
                jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role,
                    ValidAudiences = [options.Audience, "account"]
                };
                jwt.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            MapKeycloakRoles(identity, options.ResourceClient);
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(null)
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        return services;
    }

    private static void MapKeycloakRoles(ClaimsIdentity identity, string resourceClient)
    {
        foreach (var role in ExtractRoles(identity, "realm_access", null)
                     .Concat(ExtractRoles(identity, "resource_access", resourceClient)))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
            identity.AddClaim(new Claim(CurrentUser.PermissionClaim, role));
        }
    }

    private static IEnumerable<string> ExtractRoles(ClaimsIdentity identity, string claimType, string? client)
    {
        var raw = identity.FindFirst(claimType)?.Value;
        if (string.IsNullOrWhiteSpace(raw))
        {
            yield break;
        }

        using var document = JsonDocument.Parse(raw);
        var element = document.RootElement;

        if (client is not null)
        {
            if (!element.TryGetProperty(client, out element))
            {
                yield break;
            }
        }

        if (!element.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var role in roles.EnumerateArray())
        {
            var value = role.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }
    }
}
