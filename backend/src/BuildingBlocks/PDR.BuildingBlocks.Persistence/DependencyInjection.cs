using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Persistence.Behaviors;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.BuildingBlocks.Persistence.Settings;

namespace PDR.BuildingBlocks.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Wires PostgreSQL, the shared unit of work, runtime settings and startup migration for a service's
    /// <see cref="BaseDbContext"/> derivative.
    /// </summary>
    public static IServiceCollection AddPdrPersistence<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : BaseDbContext
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
                              ?? new DatabaseOptions();

        services.AddSingleton<IClock, SystemClock>();
        services.AddMemoryCache();
        services.AddScoped<IAuditContext, AuditContext>();

        services.AddDbContext<TContext>(builder => builder
            .UseNpgsql(databaseOptions.ConnectionString, npgsql =>
            {
                npgsql.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
                npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), errorCodesToAdd: null);
                npgsql.MigrationsHistoryTable("__migrations");
            })
            .UseSnakeCaseNamingConvention());

        services.AddScoped<BaseDbContext>(provider => provider.GetRequiredService<TContext>());
        services.AddScoped<ISettingsProvider, SettingsProvider>();
        services.AddScoped<ISettingsReader>(provider => provider.GetRequiredService<ISettingsProvider>());
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        services.AddHostedService<MigrationRunner<TContext>>();

        return services;
    }
}
