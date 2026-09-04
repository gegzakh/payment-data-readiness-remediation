using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Application;

namespace PDR.Remediation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRemediationApplication(this IServiceCollection services)
    {
        services.AddPdrApplication(Assembly.GetExecutingAssembly());
        return services;
    }
}
