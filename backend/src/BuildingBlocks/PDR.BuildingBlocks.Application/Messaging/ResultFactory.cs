using System.Collections.Concurrent;
using System.Reflection;
using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Results;

namespace PDR.BuildingBlocks.Application.Messaging;

/// <summary>
/// Builds a failed <see cref="Result"/> or <see cref="Result{T}"/> for an arbitrary pipeline response type,
/// so behaviours can short-circuit without knowing the concrete generic argument.
/// </summary>
public static class ResultFactory
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> FailureCache = new();

    public static bool IsResult(Type responseType) => typeof(Result).IsAssignableFrom(responseType);

    public static TResponse Failure<TResponse>(Error error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var method = FailureCache.GetOrAdd(
                responseType,
                static type => typeof(Result)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Single(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod)
                    .MakeGenericMethod(type.GetGenericArguments()[0]));

            return (TResponse)method.Invoke(null, [error])!;
        }

        throw new InvalidOperationException(
            $"Pipeline short-circuit requires a Result response, but {responseType.Name} was used.");
    }
}
