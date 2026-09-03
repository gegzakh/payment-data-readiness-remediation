using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Persistence;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.Reporting.Application.Abstractions;
using PDR.Reporting.Application.Dashboards;
using PDR.Reporting.Application.Upstream;
using PDR.Reporting.Infrastructure.Persistence;
using PDR.Reporting.Infrastructure.Upstream;

namespace PDR.Reporting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPdrPersistence<ReportingDbContext>(configuration);
        services.AddScoped<IReportingDbContext>(provider => provider.GetRequiredService<ReportingDbContext>());
        services.AddScoped<IDataSeeder, ReportingSeeder>();
        services.AddScoped<DashboardFactory>();
        services.AddScoped<SnapshotProvider>();

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

        services.AddHttpClient<IRemediationGateway, HttpRemediationGateway>(client =>
            {
                client.BaseAddress = new Uri(EnsureTrailingSlash(upstream.RemediationBaseUrl));
                client.Timeout = TimeSpan.FromSeconds(upstream.TimeoutSeconds);
            })
            .AddHttpMessageHandler<BearerForwardingHandler>();

        services.AddHttpClient<ISimulationGateway, HttpSimulationGateway>(client =>
            {
                client.BaseAddress = new Uri(EnsureTrailingSlash(upstream.SimulationBaseUrl));
                client.Timeout = TimeSpan.FromSeconds(upstream.TimeoutSeconds);
            })
            .AddHttpMessageHandler<BearerForwardingHandler>();

        return services;
    }

    private static string EnsureTrailingSlash(string url) => url.EndsWith('/') ? url : url + "/";
}
