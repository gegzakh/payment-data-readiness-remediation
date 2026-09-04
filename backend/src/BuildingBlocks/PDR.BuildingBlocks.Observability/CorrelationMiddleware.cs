using Microsoft.AspNetCore.Http;
using Serilog.Context;
using PDR.BuildingBlocks.Core.Correlation;

namespace PDR.BuildingBlocks.Observability;

/// <summary>
/// Accepts an inbound correlation id or mints one, pushes it into the ambient context, the log scope and
/// the response headers so a single id ties UI, gateway, services and messages together.
/// </summary>
public sealed class CorrelationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(CorrelationContext.HeaderName, out var header)
                            && !string.IsNullOrWhiteSpace(header)
            ? header.ToString()
            : Guid.CreateVersion7().ToString("N");

        CorrelationContext.Set(correlationId);
        context.Response.Headers[CorrelationContext.HeaderName] = correlationId;

        using (LogContext.PushProperty("correlationId", correlationId))
        using (LogContext.PushProperty("traceId", context.TraceIdentifier))
        {
            await next(context);
        }
    }
}
