using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Results;

namespace PDR.BuildingBlocks.WebApi;

public static class PdrProblemDetails
{
    public static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Concurrency => StatusCodes.Status409Conflict,
        ErrorType.Unprocessable => StatusCodes.Status422UnprocessableEntity,
        ErrorType.RateLimited => StatusCodes.Status429TooManyRequests,
        ErrorType.Dependency => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status500InternalServerError
    };

    public static ProblemDetails Create(Error error, HttpContext httpContext)
    {
        var status = StatusCodeFor(error.Type);

        var problem = new ProblemDetails
        {
            Type = $"https://pdr.dev/errors/{error.Type.ToString().ToLowerInvariant()}",
            Title = TitleFor(error.Type),
            Status = status,
            Detail = error.Message,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        problem.Extensions["correlationId"] =
            httpContext.Response.Headers[Core.Correlation.CorrelationContext.HeaderName].ToString();

        if (error.ValidationErrors.Count > 0)
        {
            problem.Extensions["errors"] = error.ValidationErrors;
        }

        return problem;
    }

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflicting state",
        ErrorType.Concurrency => "Concurrent modification",
        ErrorType.Unauthorized => "Authentication required",
        ErrorType.Forbidden => "Not permitted",
        ErrorType.Unprocessable => "Request cannot be processed",
        ErrorType.RateLimited => "Too many requests",
        ErrorType.Dependency => "Downstream dependency failed",
        _ => "Unexpected error"
    };
}

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result, HttpContext httpContext, int successStatusCode = StatusCodes.Status204NoContent) =>
        result.IsSuccess
            ? Results.StatusCode(successStatusCode)
            : Problem(result.Error, httpContext);

    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext httpContext) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : Problem(result.Error, httpContext);

    public static IResult ToCreatedResult<T>(this Result<T> result, HttpContext httpContext, Func<T, string> locationFactory) =>
        result.IsSuccess
            ? Results.Created(locationFactory(result.Value), result.Value)
            : Problem(result.Error, httpContext);

    private static IResult Problem(Error error, HttpContext httpContext)
    {
        var problem = PdrProblemDetails.Create(error, httpContext);
        return Results.Problem(problem);
    }
}
