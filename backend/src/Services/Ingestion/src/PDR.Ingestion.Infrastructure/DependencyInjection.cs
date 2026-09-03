using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Messaging;
using PDR.BuildingBlocks.Persistence;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.Ingestion.Application.Abstractions;
using PDR.Ingestion.Application.Ingest;
using PDR.Ingestion.Application.Parsing;
using PDR.Ingestion.Infrastructure.Persistence;

namespace PDR.Ingestion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIngestionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPdrPersistence<IngestionDbContext>(configuration);
        services.AddScoped<IIngestionDbContext>(provider => provider.GetRequiredService<IngestionDbContext>());
        services.AddScoped<IDataSeeder, IngestionSeeder>();
        services.AddScoped<FileSafetyInspector>();
        services.AddScoped<BatchProcessor>();
        services.AddSingleton<IAddressParser, CsvAddressParser>();
        services.AddSingleton<IAddressParser, Iso20022XmlParser>();
        services.AddPdrMessaging(configuration, typeof(DependencyInjection).Assembly);

        return services;
    }
}
