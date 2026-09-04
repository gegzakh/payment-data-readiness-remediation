using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PDR.BuildingBlocks.Persistence.Migrations;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Code-first migrations are applied on startup (BRD: automatic migration).</summary>
    public bool AutoMigrate { get; set; } = true;

    public bool SeedReferenceData { get; set; } = true;

    public int CommandTimeoutSeconds { get; set; } = 60;

    /// <summary>Advisory lock key so only one replica migrates at a time.</summary>
    public long MigrationLockKey { get; set; } = 8_246_913;
}

/// <summary>
/// Applies EF Core migrations at startup behind a PostgreSQL advisory lock, so rolling deployments with
/// several replicas cannot race each other.
/// </summary>
public sealed class MigrationRunner<TContext>(
    IServiceProvider serviceProvider,
    IOptions<DatabaseOptions> options,
    ILogger<MigrationRunner<TContext>> logger) : IHostedService
    where TContext : DbContext
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.AutoMigrate)
        {
            logger.LogInformation("Automatic migration disabled for {Context}", typeof(TContext).Name);
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var lockKey = options.Value.MigrationLockKey;

        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_lock({lockKey})", cancellationToken);

            var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            if (pending.Length == 0)
            {
                logger.LogInformation("{Context} schema is up to date", typeof(TContext).Name);
            }
            else
            {
                logger.LogInformation(
                    "Applying {Count} migration(s) to {Context}: {Migrations}",
                    pending.Length,
                    typeof(TContext).Name,
                    string.Join(", ", pending));
                await context.Database.MigrateAsync(cancellationToken);
            }

            foreach (var seeder in scope.ServiceProvider.GetServices<IDataSeeder>())
            {
                if (options.Value.SeedReferenceData)
                {
                    await seeder.SeedAsync(cancellationToken);
                }
            }
        }
        finally
        {
            await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({lockKey})", CancellationToken.None);
            await context.Database.CloseConnectionAsync();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
