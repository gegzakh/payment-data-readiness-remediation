using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Persistence;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.Simulation.Application.Abstractions;
using PDR.Simulation.Application.Cutover;
using PDR.Simulation.Application.Scenarios;
using PDR.Simulation.Application.Upstream;
using PDR.Simulation.Infrastructure.Persistence;
using PDR.Simulation.Infrastructure.Upstream;

namespace PDR.Simulation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSimulationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPdrPersistence<SimulationDbContext>(configuration);
        services.AddScoped<ISimulationDbContext>(provider => provider.GetRequiredService<SimulationDbContext>());
        services.AddScoped<IDataSeeder, SimulationSeeder>();
        services.AddScoped<SimulationRunner>();
        services.AddScoped<GoNoGoPackBuilder>();

        var upstream = configuration.GetSection(UpstreamOptions.SectionName).Get<UpstreamOptions>()
                       ?? new UpstreamOptions();

        services.AddSingleton(upstream);
        services.AddTransient<BearerForwardingHandler>();

        services.AddHttpClient<IPortfolioGateway, HttpPortfolioGateway>(client =>
            {
                client.BaseAddress = new Uri(EnsureTrailingSlash(upstream.ValidationBaseUrl));
                client.Timeout = TimeSpan.FromSeconds(upstream.TimeoutSeconds);
            })
            .AddHttpMessageHandler<BearerForwardingHandler>();

        services.AddHttpClient<IRemediationGateway, HttpRemediationGateway>(client =>
            {
                client.BaseAddress = new Uri(EnsureTrailingSlash(upstream.RemediationBaseUrl));
                client.Timeout = TimeSpan.FromSeconds(upstream.TimeoutSeconds);
            })
            .AddHttpMessageHandler<BearerForwardingHandler>();

        return services;
    }

    private static string EnsureTrailingSlash(string url) => url.EndsWith('/') ? url : url + "/";
}
