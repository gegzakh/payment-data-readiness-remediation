using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PDR.BuildingBlocks.Application.Behaviors;
using PDR.BuildingBlocks.Application.Messaging;

namespace PDR.BuildingBlocks.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the mediator, every handler and validator in <paramref name="assembly"/>, and the
    /// behaviours that every service shares (validation and logging). Order matters: behaviours run
    /// in registration order, outermost first.
    /// </summary>
    public static IServiceCollection AddPdrApplication(this IServiceCollection services, Assembly assembly)
    {
        services.AddScoped<ISender, Sender>();
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            foreach (var handlerInterface in type.GetInterfaces()
                         .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            {
                services.AddScoped(handlerInterface, type);
            }
        }

        return services;
    }
}
