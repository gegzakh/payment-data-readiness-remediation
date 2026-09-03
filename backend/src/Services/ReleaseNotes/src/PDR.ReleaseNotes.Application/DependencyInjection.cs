using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Application;
using PDR.ReleaseNotes.Application.Releases;

namespace PDR.ReleaseNotes.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReleaseNotesApplication(this IServiceCollection services)
    {
        services.AddPdrApplication(Assembly.GetExecutingAssembly());
        services.AddScoped<PageSizeResolver>();
        return services;
    }
}
