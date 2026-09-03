using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PDR.BuildingBlocks.Security;
using Testcontainers.PostgreSql;

namespace PDR.Notifications.IntegrationTests;

/// <summary>
/// Hosts the real API against a throwaway PostgreSQL container. The background worker is off so tests
/// drive dispatching explicitly and can assert on a delivery before it is retried underneath them.
/// </summary>
public class NotificationsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("pdr_notifications")
        .WithUsername("pdr")
        .WithPassword("pdr")
        .Build();

    protected virtual bool AuthenticationEnabled => false;

    public async ValueTask InitializeAsync() => await _database.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("Database:ConnectionString", _database.GetConnectionString());
        builder.UseSetting("Messaging:Enabled", "false");
        builder.UseSetting("Worker:Enabled", "false");
        builder.UseSetting("Authentication:Keycloak:Enabled", AuthenticationEnabled ? "true" : "false");
        builder.UseSetting("Authentication:Keycloak:RequireHttpsMetadata", "false");
        builder.UseSetting("Observability:SeqUrl", string.Empty);
        builder.UseSetting("Observability:OtlpEndpoint", string.Empty);

        builder.ConfigureServices(services =>
        {
            if (!AuthenticationEnabled)
            {
                services.RemoveAll<ICurrentUser>();
                services.AddScoped<ICurrentUser, TestCurrentUser>();
            }
        });
    }
}

/// <summary>Same service with Keycloak enforcement on, for the authorization tests.</summary>
public sealed class SecuredNotificationsApiFactory : NotificationsApiFactory
{
    protected override bool AuthenticationEnabled => true;
}

public sealed class TestCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public bool IsAuthenticated => true;

    public string UserId => UserName;

    public string UserName => accessor.HttpContext?.Request.Headers["X-Test-User"].FirstOrDefault() ?? "tester";

    public IReadOnlySet<string> Permissions =>
        new HashSet<string>(BuildingBlocks.Security.Permissions.All, StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> Roles => new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> LegalEntities => new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool HasPermission(string permission) => true;
}
