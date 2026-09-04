using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PDR.BuildingBlocks.Security;
using PDR.Simulation.Application.Upstream;
using Testcontainers.PostgreSql;

namespace PDR.Simulation.IntegrationTests;

/// <summary>
/// Hosts the real API against a throwaway PostgreSQL container, with the validation and remediation
/// services replaced by in-memory doubles so simulation arithmetic can be asserted against fixed inputs.
/// </summary>
public class SimulationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string UserHeader = "X-Test-User";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("pdr_simulation")
        .WithUsername("pdr")
        .WithPassword("pdr")
        .Build();

    public FakePortfolioGateway Portfolio { get; } = new();

    public FakeRemediationGateway Remediation { get; } = new();

    protected virtual bool AuthenticationEnabled => false;

    public async ValueTask InitializeAsync() => await _database.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public HttpClient CreateClientAs(string user)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(UserHeader, user);
        return client;
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

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPortfolioGateway>();
            services.RemoveAll<IRemediationGateway>();
            services.AddSingleton<IPortfolioGateway>(Portfolio);
            services.AddSingleton<IRemediationGateway>(Remediation);

            if (!AuthenticationEnabled)
            {
                services.RemoveAll<ICurrentUser>();
                services.AddScoped<ICurrentUser, HeaderCurrentUser>();
            }
        });
    }
}

/// <summary>Same service with Keycloak enforcement on, for the authorization tests.</summary>
public sealed class SecuredSimulationApiFactory : SimulationApiFactory
{
    protected override bool AuthenticationEnabled => true;
}

public sealed class HeaderCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public bool IsAuthenticated => true;

    public string UserId => UserName;

    public string UserName =>
        accessor.HttpContext?.Request.Headers[SimulationApiFactory.UserHeader].FirstOrDefault() ?? "tester";

    public IReadOnlySet<string> Permissions =>
        new HashSet<string>(BuildingBlocks.Security.Permissions.All, StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> Roles => new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> LegalEntities => new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool HasPermission(string permission) => true;
}

public sealed class FakePortfolioGateway : IPortfolioGateway
{
    public PortfolioSnapshot Snapshot { get; set; } = new(
        AssessedCount: 1000,
        ExcludedCount: 50,
        UnableToAssessCount: 25,
        CurrentRejectedCount: 120,
        FutureRejectedCount: 400,
        PaymentsAtRisk: 380,
        RulesetVersion: "2026.1",
        AsOfUtc: DateTimeOffset.UtcNow);

    public Task<PortfolioSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot);

    public Task<IReadOnlyList<PortfolioProfileRow>> GetProfileAsync(
        string dimension,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PortfolioProfileRow>>(dimension switch
        {
            "Scheme" =>
            [
                new PortfolioProfileRow("Scheme", "SEPA", 600, 60, 240, 10, 30),
                new PortfolioProfileRow("Scheme", "SWIFT", 400, 60, 160, 5, 20)
            ],
            "Source" => [new PortfolioProfileRow("Source", "CBS", 1000, 120, 400, 15, 50)],
            "Country" => [new PortfolioProfileRow("Country", "DE", 1000, 120, 400, 15, 50)],
            _ => [new PortfolioProfileRow("Issue", "MISSING_TOWN", 1000, 120, 400, 15, 50)]
        });
}

public sealed class FakeRemediationGateway : IRemediationGateway
{
    public RemediationSnapshot Snapshot { get; set; } = new(
        TotalCases: 400,
        RemediatedCases: 150,
        ApprovedCases: 50,
        OpenCases: 200,
        ExpiredExceptions: 0,
        FutureExposureOpen: 190,
        FutureExposureRemediated: 190);

    public Task<RemediationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot);
}
