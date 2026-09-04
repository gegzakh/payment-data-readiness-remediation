using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PDR.Validation.Application.Upstream;
using PDR.Validation.Domain.Assessments;
using Testcontainers.PostgreSql;

namespace PDR.Validation.IntegrationTests;

/// <summary>
/// Hosts the real API against a throwaway PostgreSQL container. Ingestion and rules are replaced by
/// in-memory doubles so the tests exercise this service's own behaviour, not the upstream ones.
/// </summary>
public class ValidationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("pdr_validation")
        .WithUsername("pdr")
        .WithPassword("pdr")
        .Build();

    public FakeIngestionGateway Ingestion { get; } = new();

    public FakeRulesGateway Rules { get; } = new();

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
            services.RemoveAll<IIngestionGateway>();
            services.RemoveAll<IRulesGateway>();
            services.AddSingleton<IIngestionGateway>(Ingestion);
            services.AddSingleton<IRulesGateway>(Rules);
        });
    }
}

/// <summary>Same service, but with Keycloak enforcement on, for the authorization tests.</summary>
public sealed class SecuredValidationApiFactory : ValidationApiFactory
{
    protected override bool AuthenticationEnabled => true;
}

public sealed class FakeIngestionGateway : IIngestionGateway
{
    private readonly Dictionary<Guid, IngestedBatch> _batches = [];
    private readonly Dictionary<Guid, List<IngestedRecord>> _records = [];

    public void Add(IngestedBatch batch, IEnumerable<IngestedRecord> records)
    {
        _batches[batch.Id] = batch;
        _records[batch.Id] = [.. records];
    }

    public Task<IngestedBatch?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_batches.GetValueOrDefault(batchId));

    public Task<IReadOnlyList<IngestedRecord>> GetRecordsAsync(Guid batchId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IngestedRecord>>(_records.GetValueOrDefault(batchId) ?? []);
}

public sealed class FakeRulesGateway : IRulesGateway
{
    public IReadOnlyList<RuleSnapshot> CurrentRules { get; set; } =
    [
        new("ADDR.CTRY.REQ", "Country", RuleCheck.Required, IssueSeverity.Error, "Country is mandatory.", null)
    ];

    public IReadOnlyList<RuleSnapshot>? FutureRules { get; set; } =
    [
        new("ADDR.CTRY.REQ", "Country", RuleCheck.Required, IssueSeverity.Error, "Country is mandatory.", null),
        new("ADDR.TOWN.REQ", "TownName", RuleCheck.Required, IssueSeverity.Error, "Town name is mandatory.", null),
        new("ADDR.STRUCT", "AddressLine", RuleCheck.StructuredOnly, IssueSeverity.Error, "Structured address required.", null)
    ];

    public Task<EffectiveRuleset?> GetEffectiveRulesetAsync(
        string schemeCode,
        DateOnly asOf,
        RuleMode mode,
        CancellationToken cancellationToken = default)
    {
        var rules = mode == RuleMode.Current ? CurrentRules : FutureRules;

        return Task.FromResult(rules is null
            ? null
            : new EffectiveRuleset(schemeCode, mode == RuleMode.Current ? 1 : 2, asOf, mode, rules));
    }
}
