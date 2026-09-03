using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.Validation.Application.Assess;
using PDR.Validation.Application.Assess.Commands;
using PDR.Validation.Application.Assess.Queries;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Api.Endpoints;

public static class ValidationEndpoints
{
    public static IEndpointRouteBuilder MapValidationEndpoints(this IEndpointRouteBuilder app)
    {
        var runs = app.MapGroup("/api/v1/validation/runs").WithTags("Validation");

        runs.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                int page = 1,
                int? pageSize = null,
                Guid? batchId = null,
                string? sourceCode = null,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(
                    new GetRunsQuery(page, pageSize, batchId, sourceCode),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Validation.Read)
            .WithName("GetValidationRuns")
            .WithSummary("Validation runs with their readiness, exposure and reconciliation counts.")
            .Produces<PagedResult<ValidationRunDto>>();

        runs.MapGet("/{runId:guid}", async (
                Guid runId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetRunByIdQuery(runId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Validation.Read)
            .WithName("GetValidationRunById")
            .Produces<ValidationRunDto>();

        runs.MapGet("/{runId:guid}/assessments", async (
                Guid runId,
                HttpContext httpContext,
                ISender sender,
                int page = 1,
                int? pageSize = null,
                RecordOutcome? outcome = null,
                RuleMode mode = RuleMode.Future,
                AddressClassification? classification = null,
                string? ruleCode = null,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(
                    new GetRunAssessmentsQuery(runId, page, pageSize, outcome, mode, classification, ruleCode),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Validation.Read)
            .WithName("GetValidationAssessments")
            .WithSummary("Assessed records and their findings; masked unless the caller may drill down.")
            .Produces<PagedResult<AddressAssessmentDto>>();

        runs.MapPost("/", async (
                HttpContext httpContext,
                ISender sender,
                RunValidationRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new RunValidationCommand(request.BatchId, request.AsOf),
                    cancellationToken);

                return result.ToCreatedResult(httpContext, run => $"/api/v1/validation/runs/{run.Id}");
            })
            .RequireAuthorization(Permissions.Validation.Run)
            .WithName("RunValidation")
            .WithSummary("Validates a parsed ingestion batch against the current and post-cutover rule sets.")
            .Produces<ValidationRunDto>(StatusCodes.Status201Created);

        var validation = app.MapGroup("/api/v1/validation").WithTags("Validation");

        validation.MapGet("/readiness", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetReadinessSummaryQuery(), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Validation.Read)
            .WithName("GetReadinessSummary")
            .WithSummary("Portfolio readiness today and after the cutover, with the payments at risk.")
            .Produces<ReadinessSummaryDto>();

        validation.MapGet("/profile", async (
                HttpContext httpContext,
                ISender sender,
                ProfileDimension dimension = ProfileDimension.Scheme,
                Guid? runId = null,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(new GetProfileQuery(dimension, runId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Validation.Read)
            .WithName("GetValidationProfile")
            .WithSummary("Breaks exposure down by scheme, source, party role, country, classification or issue.")
            .Produces<ProfileDto>();

        var internalRuns = app.MapGroup("/internal/v1/validation/runs").WithTags("Internal").ExcludeFromDescription();

        internalRuns.MapGet("/latest", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetLatestRunQuery(), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Write)
            .WithName("GetLatestRunForRemediation");

        internalRuns.MapGet("/{runId:guid}", async (
                Guid runId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetRunByIdQuery(runId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Write)
            .WithName("GetRunForRemediation");

        internalRuns.MapGet("/{runId:guid}/assessments", async (
                Guid runId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new ExportRunAssessmentsQuery(runId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Write)
            .WithName("ExportRunAssessmentsForRemediation");

        return app;
    }
}

public sealed record RunValidationRequest(Guid BatchId, DateOnly? AsOf);
