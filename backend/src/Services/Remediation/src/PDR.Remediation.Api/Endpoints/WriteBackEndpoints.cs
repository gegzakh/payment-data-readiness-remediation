using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.Remediation.Application.WriteBack;

namespace PDR.Remediation.Api.Endpoints;

public static class WriteBackEndpoints
{
    public static IEndpointRouteBuilder MapWriteBackEndpoints(this IEndpointRouteBuilder app)
    {
        var writeBack = app.MapGroup("/api/v1/remediation/writeback").WithTags("Write-back");

        writeBack.MapGet("/targets", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetWriteBackTargetsQuery(), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Read)
            .WithName("GetWriteBackTargets")
            .WithSummary("Sources that may be written to, with their fields, windows, limits and rollback method.")
            .Produces<IReadOnlyList<WriteBackTargetDto>>();

        writeBack.MapPost("/preview", async (
                HttpContext httpContext,
                ISender sender,
                WriteBackRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new PreviewWriteBackCommand(request.SourceCode, request.CaseIds),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Read)
            .WithName("PreviewWriteBack")
            .WithSummary("Field-level before and after values a write-back would apply.")
            .Produces<WriteBackPreviewDto>();

        writeBack.MapPost("/apply", async (
                HttpContext httpContext,
                ISender sender,
                WriteBackRequest request,
                CancellationToken cancellationToken) =>
            {
                var idempotencyKey = request.IdempotencyKey
                                     ?? httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();

                var result = await sender.SendAsync(
                    new ApplyWriteBackCommand(request.SourceCode, request.CaseIds, idempotencyKey),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.WriteBack)
            .WithName("ApplyWriteBack")
            .WithSummary("Applies approved corrections to the source; replaying an idempotency key returns the first job.")
            .Produces<WriteBackJobDto>();

        writeBack.MapGet("/jobs", async (
                HttpContext httpContext,
                ISender sender,
                int page = 1,
                int? pageSize = null,
                string? sourceCode = null,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(
                    new GetWriteBackJobsQuery(page, pageSize, sourceCode),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Read)
            .WithName("GetWriteBackJobs")
            .Produces<PagedResult<WriteBackJobDto>>();

        writeBack.MapGet("/jobs/{jobId:guid}", async (
                Guid jobId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetWriteBackJobByIdQuery(jobId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Read)
            .WithName("GetWriteBackJobById")
            .Produces<WriteBackJobDto>();

        writeBack.MapGet("/jobs/{jobId:guid}/reconciliation", async (
                Guid jobId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new ReconcileWriteBackQuery(jobId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Read)
            .WithName("ReconcileWriteBackJob")
            .WithSummary("Re-reads the source and reports any record the job cannot account for.")
            .Produces<WriteBackReconciliationDto>();

        writeBack.MapPost("/jobs/{jobId:guid}/rollback", async (
                Guid jobId,
                HttpContext httpContext,
                ISender sender,
                RollbackRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new RollbackWriteBackCommand(jobId, request.Reason),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.WriteBack)
            .WithName("RollbackWriteBackJob")
            .WithSummary("Restores the original values in the source and reopens the affected cases.")
            .Produces<WriteBackJobDto>();

        return app;
    }
}

public sealed record WriteBackRequest(string SourceCode, IReadOnlyList<Guid>? CaseIds, string? IdempotencyKey);

public sealed record RollbackRequest(string Reason);
