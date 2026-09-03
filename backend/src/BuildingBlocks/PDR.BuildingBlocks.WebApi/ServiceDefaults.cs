using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using PDR.BuildingBlocks.Observability;
using PDR.BuildingBlocks.Security;
using Scalar.AspNetCore;

namespace PDR.BuildingBlocks.WebApi;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    public string Title { get; set; } = "PDR Service";

    public string Version { get; set; } = "v1";

    public int RateLimitPermitPerMinute { get; set; } = 600;

    public string[] AllowedOrigins { get; set; } = [];
}

public static class ServiceDefaults
{
    public const string CorsPolicy = "pdr-frontends";

    /// <summary>
    /// The single composition point every service calls: logging, tracing, authentication, authorization,
    /// problem details, CORS, rate limiting, OpenAPI and health checks are configured identically.
    /// </summary>
    public static IHostApplicationBuilder AddPdrService(this IHostApplicationBuilder builder, string serviceName)
    {
        builder.AddPdrObservability(serviceName);

        builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));
        var apiOptions = builder.Configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>() ?? new ApiOptions();

        builder.Services.ConfigureHttpJsonOptions(json =>
            json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services.AddPdrAuthentication(builder.Configuration);
        builder.Services.AddHealthChecks();

        builder.Services.AddCors(cors => cors.AddPolicy(CorsPolicy, policy =>
        {
            if (apiOptions.AllowedOrigins.Length > 0)
            {
                policy.WithOrigins(apiOptions.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders(Core.Correlation.CorrelationContext.HeaderName);
            }
        }));

        builder.Services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = apiOptions.RateLimitPermitPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return builder;
    }

    /// <summary>Applies the shared middleware pipeline in the one correct order.</summary>
    public static WebApplication UsePdrDefaults(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UsePdrCorrelation();

        // Authentication and authorization short-circuit before any handler runs, so without this a
        // client gets a bodyless 401/403 instead of the ProblemDetails every other failure returns.
        app.UseStatusCodePages(context =>
        {
            var response = context.HttpContext.Response;
            if (response.StatusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden))
            {
                return Task.CompletedTask;
            }

            var error = response.StatusCode == StatusCodes.Status401Unauthorized
                ? Core.Errors.Error.Unauthorized("AUTH.UNAUTHENTICATED", "Authentication is required for this endpoint.")
                : Core.Errors.Error.Forbidden("AUTH.FORBIDDEN", "The caller does not hold the required permission.");

            var problem = PdrProblemDetails.Create(error, context.HttpContext);
            return response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        });

        app.UseCors(CorsPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapOpenApi();
        app.MapScalarApiReference(options => options.WithTitle(app.Environment.ApplicationName));

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        }).AllowAnonymous();

        return app;
    }
}
