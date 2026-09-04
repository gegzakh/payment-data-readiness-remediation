using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Application;
using PDR.Notifications.Application.Notifications;
using PDR.Notifications.Application.Schedules;

namespace PDR.Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddPdrApplication(Assembly.GetExecutingAssembly());
        services.AddScoped<DeliveryDispatcher>();
        services.AddScoped<ScheduledReportRunner>();
        return services;
    }
}
