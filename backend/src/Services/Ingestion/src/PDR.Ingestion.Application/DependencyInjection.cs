using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Application;

namespace PDR.Ingestion.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIngestionApplication(this IServiceCollection services)
    {
        services.AddPdrApplication(Assembly.GetExecutingAssembly());
        return services;
    }
}
