using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Core.Results;

namespace PDR.BuildingBlocks.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICorrelationContext correlationContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["request"] = requestName,
            ["correlationId"] = correlationContext.CorrelationId
        });

        try
        {
            var response = await next();
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (response is Result { IsFailure: true } failed)
            {
                logger.LogWarning(
                    "{Request} rejected with {ErrorCode} ({ErrorType}) in {ElapsedMs}ms",
                    requestName,
                    failed.Error.Code,
                    failed.Error.Type,
                    elapsedMs);
            }
            else if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("{Request} handled in {ElapsedMs}ms", requestName, elapsedMs);
            }

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            logger.LogError(
                exception,
                "{Request} failed unexpectedly after {ElapsedMs}ms",
                requestName,
                elapsedMs);
            throw;
        }
    }
}
