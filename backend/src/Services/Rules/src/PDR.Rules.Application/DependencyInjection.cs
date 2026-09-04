using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Application;

namespace PDR.Rules.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRulesApplication(this IServiceCollection services)
    {
        services.AddPdrApplication(Assembly.GetExecutingAssembly());
        return services;
    }
}
