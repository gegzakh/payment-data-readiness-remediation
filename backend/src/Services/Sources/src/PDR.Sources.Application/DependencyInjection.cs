using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Application;

namespace PDR.Sources.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSourcesApplication(this IServiceCollection services)
    {
        services.AddPdrApplication(Assembly.GetExecutingAssembly());
        return services;
    }
}
