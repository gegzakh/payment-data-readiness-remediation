using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Application;

namespace PDR.Audit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuditApplication(this IServiceCollection services)
    {
        services.AddPdrApplication(Assembly.GetExecutingAssembly());
        return services;
    }
}
