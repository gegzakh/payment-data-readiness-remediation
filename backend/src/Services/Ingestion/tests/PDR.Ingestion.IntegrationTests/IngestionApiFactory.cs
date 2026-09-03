using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace PDR.Ingestion.IntegrationTests;

/// <summary>
/// Hosts the real API against a throwaway PostgreSQL container, so migrations, seeding and the
/// full HTTP pipeline are exercised exactly as in production.
/// </summary>
public class IngestionApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("pdr_ingestion")
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
        builder.UseSetting("Authentication:Keycloak:Enabled", AuthenticationEnabled ? "true" : "false");
        builder.UseSetting("Authentication:Keycloak:RequireHttpsMetadata", "false");
        builder.UseSetting("Observability:SeqUrl", string.Empty);
        builder.UseSetting("Observability:OtlpEndpoint", string.Empty);
    }
}

/// <summary>Same service, but with Keycloak enforcement on, for the authorization tests.</summary>
public sealed class SecuredIngestionApiFactory : IngestionApiFactory
{
    protected override bool AuthenticationEnabled => true;
}
