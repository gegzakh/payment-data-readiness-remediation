using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Persistence;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.Remediation.Application.Abstractions;
using PDR.Remediation.Application.Cases;
using PDR.Remediation.Application.Upstream;
using PDR.Remediation.Application.WriteBack;
using PDR.Remediation.Infrastructure.Persistence;
using PDR.Remediation.Infrastructure.Upstream;
using PDR.Remediation.Infrastructure.WriteBack;

namespace PDR.Remediation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRemediationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPdrPersistence<RemediationDbContext>(configuration);
        services.AddScoped<IRemediationDbContext>(provider => provider.GetRequiredService<RemediationDbContext>());
        services.AddScoped<IDataSeeder, RemediationSeeder>();
        services.AddScoped<CaseGenerator>();
        services.AddScoped<WriteBackService>();
        services.AddScoped<IWriteBackConnector, SimulatedWriteBackConnector>();

        var upstream = configuration.GetSection(UpstreamOptions.SectionName).Get<UpstreamOptions>()
                       ?? new UpstreamOptions();

        services.AddSingleton(upstream);
        services.AddTransient<BearerForwardingHandler>();

        services.AddHttpClient<IValidationGateway, HttpValidationGateway>(client =>
            {
                client.BaseAddress = new Uri(EnsureTrailingSlash(upstream.ValidationBaseUrl));
                client.Timeout = TimeSpan.FromSeconds(upstream.TimeoutSeconds);
            })
            .AddHttpMessageHandler<BearerForwardingHandler>();

        services.AddHttpClient<ISourcesGateway, HttpSourcesGateway>(client =>
            {
                client.BaseAddress = new Uri(EnsureTrailingSlash(upstream.SourcesBaseUrl));
                client.Timeout = TimeSpan.FromSeconds(upstream.TimeoutSeconds);
            })
            .AddHttpMessageHandler<BearerForwardingHandler>();

        return services;
    }

    private static string EnsureTrailingSlash(string url) => url.EndsWith('/') ? url : url + "/";
}
