using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PDR.BuildingBlocks.Core.Errors;

namespace PDR.BuildingBlocks.WebApi;

/// <summary>
/// Single funnel for unhandled exceptions: every service answers with RFC 9457 problem+json and never
/// leaks stack traces, SQL or personal data to the caller.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            var error = new Error(
                "COMMON.UNEXPECTED_ERROR",
                "An unexpected error occurred. Quote the correlation id when reporting this.",
                ErrorType.Failure);

            var problem = PdrProblemDetails.Create(error, context);
            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
