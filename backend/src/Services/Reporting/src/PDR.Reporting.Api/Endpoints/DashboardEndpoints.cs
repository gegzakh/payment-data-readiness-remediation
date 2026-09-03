using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.Reporting.Application.Dashboards;
using PDR.Reporting.Domain.Dashboards;

namespace PDR.Reporting.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var reporting = app.MapGroup("/api/v1/reporting").WithTags("Reporting");

        reporting.MapGet("/dashboards", (HttpContext httpContext) =>
            {
                _ = httpContext;
                return Results.Ok(Enum.GetValues<DashboardAudience>().Select(audience => new
                {
                    Audience = audience,
                    Key = audience.ToString().ToLowerInvariant()
                }));
            })
            .RequireAuthorization(Permissions.Reporting.Read)
            .WithName("GetDashboardCatalogue")
            .WithSummary("The dashboards this service can produce.");

        reporting.MapGet("/dashboards/{audience}", async (
                string audience,
                string? schemeCodes,
                string? sourceCodes,
                string? countries,
                string? exclusions,
                DateOnly? asOf,
                bool? refresh,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!TryParseAudience(audience, out var parsed))
                {
                    return UnknownAudience(audience);
                }

                var query = new GetDashboardQuery(
                    parsed,
                    new DashboardScopeRequest(schemeCodes, sourceCodes, countries, exclusions, asOf),
                    refresh ?? false);

                var result = await sender.SendAsync(query, cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Reporting.Read)
            .WithName("GetDashboard")
            .WithSummary("A dashboard for the given audience and scope, stamped with its freshness and ruleset.")
            .Produces<DashboardDto>();

        reporting.MapGet("/dashboards/{audience}/drill/{dimension}", async (
                string audience,
                string dimension,
                string? schemeCodes,
                string? sourceCodes,
                string? countries,
                string? exclusions,
                DateOnly? asOf,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!TryParseAudience(audience, out var parsed))
                {
                    return UnknownAudience(audience);
                }

                var query = new GetDrillDownQuery(
                    parsed,
                    dimension,
                    new DashboardScopeRequest(schemeCodes, sourceCodes, countries, exclusions, asOf));

                var result = await sender.SendAsync(query, cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Reporting.Read)
            .WithName("DrillDownDashboard")
            .WithSummary("The rows behind a metric, for one dimension.")
            .Produces<DrillDownDto>();

        reporting.MapGet("/dashboards/{audience}/export", async (
                string audience,
                string? schemeCodes,
                string? sourceCodes,
                string? countries,
                string? exclusions,
                DateOnly? asOf,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!TryParseAudience(audience, out var parsed))
                {
                    return UnknownAudience(audience);
                }

                var result = await sender.SendAsync(
                    new ExportDashboardQuery(
                        parsed,
                        new DashboardScopeRequest(schemeCodes, sourceCodes, countries, exclusions, asOf)),
                    cancellationToken);

                if (result.IsFailure)
                {
                    return result.ToHttpResult(httpContext);
                }

                var export = result.Value;
                return Results.File(export.Content, export.ContentType, export.FileName);
            })
            .RequireAuthorization(Permissions.Reporting.Export)
            .WithName("ExportDashboard")
            .WithSummary("The dashboard as CSV, with its scope and freshness in the header rows.");

        reporting.MapGet("/snapshots", async (
                string? audience,
                int? page,
                int? pageSize,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                DashboardAudience? filter = null;
                if (!string.IsNullOrWhiteSpace(audience))
                {
                    if (!TryParseAudience(audience, out var parsed))
                    {
                        return UnknownAudience(audience);
                    }

                    filter = parsed;
                }

                var result = await sender.SendAsync(
                    new GetSnapshotHistoryQuery(filter, page ?? 1, pageSize),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Reporting.Read)
            .WithName("GetSnapshotHistory")
            .WithSummary("Previously captured snapshots, newest first.")
            .Produces<PagedResult<DashboardDto>>();

        return app;
    }

    /// <summary>Audiences travel in URLs as their lower-case catalogue key, so parsing ignores case.</summary>
    private static bool TryParseAudience(string value, out DashboardAudience audience) =>
        Enum.TryParse(value, ignoreCase: true, out audience) && Enum.IsDefined(audience);

    private static IResult UnknownAudience(string value) =>
        Results.Problem(
            title: "Unknown dashboard audience",
            detail: $"'{value}' is not a dashboard audience.",
            statusCode: StatusCodes.Status400BadRequest);
}
