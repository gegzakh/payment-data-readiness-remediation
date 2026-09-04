using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace PDR.BuildingBlocks.Application.Messaging;

/// <summary>
/// Minimal in-process mediator. Deliberately hand-rolled instead of taking a commercially licensed
/// mediator dependency; behaviour composition is identical (see docs/architecture/LOW_LEVEL_DESIGN.md §3).
/// </summary>
public sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> InvokerCache = new();

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var invoker = InvokerCache.GetOrAdd(
            requestType,
            static type => typeof(Sender)
                .GetMethod(nameof(Invoke), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(type, typeof(TResponse)));

        return (Task<TResponse>)invoker.Invoke(this, [request, cancellationToken])!;
    }

    private Task<TResponse> Invoke<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var handlerType = HandlerTypeCache.GetOrAdd(
            typeof(TRequest),
            static type => typeof(IRequestHandler<,>).MakeGenericType(type, typeof(TResponse)));

        var handler = (IRequestHandler<TRequest, TResponse>?)serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {typeof(TRequest).Name}.");

        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .ToArray();

        Func<Task<TResponse>> pipeline = () => handler.HandleAsync(request, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(request, next, cancellationToken);
        }

        return pipeline();
    }
}
