using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Messaging;
using PDR.BuildingBlocks.Persistence;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.Validation.Application.Abstractions;
using PDR.Validation.Application.Assess;
using PDR.Validation.Application.Upstream;
using PDR.Validation.Infrastructure.Persistence;
using PDR.Validation.Infrastructure.Upstream;

namespace PDR.Validation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddValidationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPdrPersistence<ValidationDbContext>(configuration);
        services.AddScoped<IValidationDbContext>(provider => provider.GetRequiredService<ValidationDbContext>());
        services.AddScoped<IDataSeeder, ValidationSeeder>();
        services.AddScoped<ValidationEngine>();

        var upstream = configuration.GetSection(UpstreamOptions.SectionName).Get<UpstreamOptions>()
                       ?? new UpstreamOptions();

        services.AddSingleton(upstream);
        services.AddTransient<BearerForwardingHandler>();

        services.AddHttpClient<IIngestionGateway, HttpIngestionGateway>(client =>
            {
                client.BaseAddress = new Uri(EnsureTrailingSlash(upstream.IngestionBaseUrl));
                client.Timeout = TimeSpan.FromSeconds(upstream.TimeoutSeconds);
            })
            .AddHttpMessageHandler<BearerForwardingHandler>();

        services.AddHttpClient<IRulesGateway, HttpRulesGateway>(client =>
            {
                client.BaseAddress = new Uri(EnsureTrailingSlash(upstream.RulesBaseUrl));
                client.Timeout = TimeSpan.FromSeconds(upstream.TimeoutSeconds);
            })
            .AddHttpMessageHandler<BearerForwardingHandler>();

        services.AddPdrMessaging(configuration, typeof(DependencyInjection).Assembly);

        return services;
    }

    private static string EnsureTrailingSlash(string url) => url.EndsWith('/') ? url : url + "/";
}
