using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Persistence;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.Notifications.Application.Abstractions;
using PDR.Notifications.Infrastructure.Channels;
using PDR.Notifications.Infrastructure.Persistence;
using PDR.Notifications.Infrastructure.Workers;

namespace PDR.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPdrPersistence<NotificationsDbContext>(configuration);
        services.AddScoped<INotificationsDbContext>(provider => provider.GetRequiredService<NotificationsDbContext>());
        services.AddScoped<IDataSeeder, NotificationsSeeder>();

        services.AddScoped<IChannelSender, InAppChannelSender>();
        services.AddScoped<IChannelSender, EmailChannelSender>();

        var delivery = configuration.GetSection(DeliveryOptions.SectionName).Get<DeliveryOptions>() ?? new DeliveryOptions();
        services.AddSingleton(delivery);

        services.AddHttpClient<IChannelSender, WebhookChannelSender>(client =>
            client.Timeout = TimeSpan.FromSeconds(delivery.TimeoutSeconds));
        services.AddHttpClient<IChannelSender, ItsmTaskChannelSender>(client =>
            client.Timeout = TimeSpan.FromSeconds(delivery.TimeoutSeconds));

        var worker = configuration.GetSection(NotificationWorkerOptions.SectionName).Get<NotificationWorkerOptions>()
                     ?? new NotificationWorkerOptions();
        services.AddSingleton(worker);
        services.AddHostedService<NotificationWorker>();

        return services;
    }
}

public sealed class DeliveryOptions
{
    public const string SectionName = "Delivery";

    public int TimeoutSeconds { get; set; } = 15;
}
