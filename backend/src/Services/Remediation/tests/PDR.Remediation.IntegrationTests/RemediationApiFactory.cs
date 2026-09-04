using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PDR.BuildingBlocks.Security;
using PDR.Remediation.Application.Upstream;
using Testcontainers.PostgreSql;

namespace PDR.Remediation.IntegrationTests;

/// <summary>
/// Hosts the real API against a throwaway PostgreSQL container. Validation and sources are replaced by
/// in-memory doubles, and the caller's identity comes from a header so maker-checker separation can be
/// exercised without a live Keycloak.
/// </summary>
public class RemediationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string UserHeader = "X-Test-User";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("pdr_remediation")
        .WithUsername("pdr")
        .WithPassword("pdr")
        .Build();

    public FakeValidationGateway Validation { get; } = new();

    public FakeSourcesGateway Sources { get; } = new();

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
            services.RemoveAll<IValidationGateway>();
            services.RemoveAll<ISourcesGateway>();
            services.AddSingleton<IValidationGateway>(Validation);
            services.AddSingleton<ISourcesGateway>(Sources);

            if (!AuthenticationEnabled)
            {
                services.RemoveAll<ICurrentUser>();
                services.AddScoped<ICurrentUser, HeaderCurrentUser>();
            }
        });
    }
}

/// <summary>Same service with Keycloak enforcement on, for the authorization tests.</summary>
public sealed class SecuredRemediationApiFactory : RemediationApiFactory
{
    protected override bool AuthenticationEnabled => true;
}

/// <summary>Takes the caller's name from a request header so tests can act as maker and checker.</summary>
public sealed class HeaderCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public bool IsAuthenticated => true;

    public string UserId => UserName;

    public string UserName =>
        accessor.HttpContext?.Request.Headers[RemediationApiFactory.UserHeader].FirstOrDefault() ?? "tester";

    public IReadOnlySet<string> Permissions =>
        new HashSet<string>(BuildingBlocks.Security.Permissions.All, StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> Roles => new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> LegalEntities => new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool HasPermission(string permission) => true;
}

public sealed class FakeValidationGateway : IValidationGateway
{
    private readonly Dictionary<Guid, ValidationRunSummary> _runs = [];
    private readonly Dictionary<Guid, List<AssessedAddress>> _assessments = [];

    public void Add(ValidationRunSummary run, IEnumerable<AssessedAddress> assessments)
    {
        _runs[run.Id] = run;
        _assessments[run.Id] = [.. assessments];
        Latest = run;
    }

    public ValidationRunSummary? Latest { get; private set; }

    public Task<ValidationRunSummary?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_runs.GetValueOrDefault(runId));

    public Task<ValidationRunSummary?> GetLatestRunAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Latest);

    public Task<IReadOnlyList<AssessedAddress>> GetAssessmentsAsync(
        Guid runId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AssessedAddress>>(_assessments.GetValueOrDefault(runId) ?? []);
}

public sealed class FakeSourcesGateway : ISourcesGateway
{
    public Task<SourceOwner?> GetOwnerAsync(string sourceCode, CancellationToken cancellationToken = default) =>
        Task.FromResult<SourceOwner?>(new SourceOwner(sourceCode, "Data Team", "data@example.local"));
}
