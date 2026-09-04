using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Messaging;
using PDR.BuildingBlocks.Persistence;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.Audit.Application.Abstractions;
using PDR.Audit.Infrastructure.Persistence;

namespace PDR.Audit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuditInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPdrPersistence<AuditDbContext>(configuration);
        services.AddScoped<IAuditDbContext>(provider => provider.GetRequiredService<AuditDbContext>());
        services.AddScoped<IDataSeeder, AuditSeeder>();
        services.AddScoped<IAuditChainLock, PostgresAuditChainLock>();
        services.AddPdrMessaging(configuration, typeof(DependencyInjection).Assembly);

        return services;
    }
}
