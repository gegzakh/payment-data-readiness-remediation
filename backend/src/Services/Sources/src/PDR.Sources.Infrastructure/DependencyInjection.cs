using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Messaging;
using PDR.BuildingBlocks.Persistence;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.Sources.Application.Abstractions;
using PDR.Sources.Application.Inventory;
using PDR.Sources.Infrastructure.Persistence;

namespace PDR.Sources.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSourcesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPdrPersistence<SourcesDbContext>(configuration);
        services.AddScoped<ISourcesDbContext>(provider => provider.GetRequiredService<SourcesDbContext>());
        services.AddScoped<IDataSeeder, SourcesSeeder>();
        services.AddScoped<SourceReadinessPolicy>();
        services.AddPdrMessaging(configuration, typeof(DependencyInjection).Assembly);

        return services;
    }
}
