using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Messaging;
using PDR.BuildingBlocks.Persistence;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.ReleaseNotes.Application.Abstractions;
using PDR.ReleaseNotes.Infrastructure.Persistence;

namespace PDR.ReleaseNotes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReleaseNotesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPdrPersistence<ReleaseNotesDbContext>(configuration);
        services.AddScoped<IReleaseNotesDbContext>(provider =>
            provider.GetRequiredService<ReleaseNotesDbContext>());
        services.AddScoped<IDataSeeder, ReleaseNotesSeeder>();
        services.AddPdrMessaging(configuration, typeof(DependencyInjection).Assembly);

        return services;
    }
}
