using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Messaging;
using PDR.BuildingBlocks.Persistence;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.Rules.Application.Abstractions;
using PDR.Rules.Infrastructure.Persistence;

namespace PDR.Rules.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRulesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPdrPersistence<RulesDbContext>(configuration);
        services.AddScoped<IRulesDbContext>(provider => provider.GetRequiredService<RulesDbContext>());
        services.AddScoped<IDataSeeder, RulesSeeder>();
        services.AddPdrMessaging(configuration, typeof(DependencyInjection).Assembly);

        return services;
    }
}
