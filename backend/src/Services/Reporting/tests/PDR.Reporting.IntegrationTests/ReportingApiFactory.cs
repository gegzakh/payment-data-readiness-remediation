using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PDR.BuildingBlocks.Security;
using PDR.Reporting.Application.Upstream;
using Testcontainers.PostgreSql;

namespace PDR.Reporting.IntegrationTests;

/// <summary>
/// Hosts the real API against a throwaway PostgreSQL container with in-memory upstreams, so dashboard
/// numbers can be asserted against fixed inputs.
/// </summary>
public class ReportingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("pdr_reporting")
        .WithUsername("pdr")
        .WithPassword("pdr")
        .Build();

    public FakeValidationGateway Validation { get; } = new();

    public FakeRemediationGateway Remediation { get; } = new();

    public FakeSimulationGateway Simulation { get; } = new();

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

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IValidationGateway>();
            services.RemoveAll<IRemediationGateway>();
            services.RemoveAll<ISimulationGateway>();
            services.AddSingleton<IValidationGateway>(Validation);
            services.AddSingleton<IRemediationGateway>(Remediation);
            services.AddSingleton<ISimulationGateway>(Simulation);

            if (!AuthenticationEnabled)
            {
                services.RemoveAll<ICurrentUser>();
                services.AddScoped<ICurrentUser, TestCurrentUser>();
            }
        });
    }
}

/// <summary>Same service with Keycloak enforcement on, for the authorization tests.</summary>
public sealed class SecuredReportingApiFactory : ReportingApiFactory
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

public sealed class FakeValidationGateway : IValidationGateway
{
    public ValidationSnapshot Snapshot { get; set; } = new(
        AssessedCount: 1000,
        ExcludedCount: 40,
        UnableToAssessCount: 10,
        CurrentRejectedCount: 100,
        FutureRejectedCount: 250,
        CurrentWarningCount: 30,
        FutureWarningCount: 60,
        PaymentsAtRisk: 240,
        RulesetVersion: "2026.1",
        AsOfUtc: DateTimeOffset.UtcNow);

    public Task<ValidationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot);

    public Task<IReadOnlyList<ValidationProfileRow>> GetProfileAsync(
        string dimension,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ValidationProfileRow>>(dimension switch
        {
            "Scheme" =>
            [
                new ValidationProfileRow("Scheme", "SEPA", 600, 60, 240, 20, 40),
                new ValidationProfileRow("Scheme", "SWIFT", 400, 40, 10, 10, 20)
            ],
            "Source" => [new ValidationProfileRow("Source", "CBS", 1000, 100, 250, 30, 60)],
            "Country" => [new ValidationProfileRow("Country", "DE", 1000, 100, 250, 30, 60)],
            _ => [new ValidationProfileRow("Issue", "MISSING_TOWN", 1000, 100, 250, 30, 60)]
        });
}

public sealed class FakeRemediationGateway : IRemediationGateway
{
    public RemediationSnapshot Snapshot { get; set; } = new(200, 80, 20, 100, 3, 90, 150);

    public Task<RemediationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot);
}

public sealed class FakeSimulationGateway : ISimulationGateway
{
    public SimulationSnapshot Snapshot { get; set; } = new(
        LatestRunId: Guid.CreateVersion7(),
        LatestRunScenario: "REMEDIATED",
        LatestRunAtUtc: DateTimeOffset.UtcNow,
        RemediatedRejectedCount: 60,
        RemediatedPaymentsAtRisk: 55,
        RemediatedReadinessPercent: 94m,
        Recommendation: "Go",
        ResidualExposure: 55,
        EntryCriteriaOutstanding: 1,
        ExitCriteriaOutstanding: 2,
        WaivedCriteria: 1,
        OpenDefects: 4,
        UatMismatches: 2,
        TestCoveragePercent: 88.5m,
        RulesetVersion: "2026.1");

    public Task<SimulationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot);
}
