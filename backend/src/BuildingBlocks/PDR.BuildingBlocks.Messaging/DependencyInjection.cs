using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Messaging.Outbox;

namespace PDR.BuildingBlocks.Messaging;

public static class DependencyInjection
{
    /// <summary>
    /// Configures RabbitMQ transport with the retry/redelivery policy shared by all services, plus the
    /// background outbox publisher.
    /// </summary>
    public static IServiceCollection AddPdrMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] consumerAssemblies)
    {
        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));
        var options = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();

        if (!options.Enabled)
        {
            return services;
        }

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            if (consumerAssemblies.Length > 0)
            {
                bus.AddConsumers(consumerAssemblies);
            }

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, options.Port, options.VirtualHost, host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });

                cfg.PrefetchCount = options.PrefetchCount;
                cfg.UseMessageRetry(retry => retry.Interval(options.RetryCount, TimeSpan.FromSeconds(options.RetryIntervalSeconds)));
                cfg.UseCircuitBreaker(breaker =>
                {
                    breaker.TrackingPeriod = TimeSpan.FromMinutes(1);
                    breaker.TripThreshold = 25;
                    breaker.ActiveThreshold = 10;
                    breaker.ResetInterval = TimeSpan.FromMinutes(1);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddHostedService<OutboxPublisher>();

        return services;
    }
}
