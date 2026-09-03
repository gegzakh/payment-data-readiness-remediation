using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.Ingestion.Application.Ingest;
using PDR.Ingestion.Application.Ingest.Commands;
using PDR.Ingestion.Application.Ingest.Queries;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Api.Endpoints;

public static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        var batches = app.MapGroup("/api/v1/batches").WithTags("Ingestion");

        batches.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                int page = 1,
                int? pageSize = null,
                BatchStatus? status = null,
                string? sourceCode = null,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(
                    new GetBatchesQuery(page, pageSize, status, sourceCode),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Ingestion.Read)
            .WithName("GetBatches")
            .WithSummary("Ingestion batches with their provenance, counts and reconciliation state.")
            .Produces<PagedResult<IngestionBatchDto>>();

        batches.MapGet("/overview", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetIngestionOverviewQuery(), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Ingestion.Read)
            .WithName("GetIngestionOverview")
            .Produces<IngestionOverviewDto>();

        batches.MapGet("/{batchId:guid}", async (
                Guid batchId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetBatchByIdQuery(batchId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Ingestion.Read)
            .WithName("GetBatchById")
            .Produces<IngestionBatchDto>();

        batches.MapGet("/{batchId:guid}/records", async (
                Guid batchId,
                HttpContext httpContext,
                ISender sender,
                int page = 1,
                int? pageSize = null,
                bool duplicatesOnly = false,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(
                    new GetBatchRecordsQuery(batchId, page, pageSize, duplicatesOnly),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Ingestion.Read)
            .WithName("GetBatchRecords")
            .WithSummary("Party addresses parsed from a batch; masked unless the caller may drill down.")
            .Produces<PagedResult<PartyAddressRecordDto>>();

        batches.MapPost("/upload", async (
                HttpContext httpContext,
                ISender sender,
                IFormFile file,
                string sourceCode,
                IngestionFormat format,
                IngestionChannel channel = IngestionChannel.Upload,
                bool reprocess = false,
                string? idempotencyKey = null,
                CancellationToken cancellationToken = default) =>
            {
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer, cancellationToken);

                var result = await sender.SendAsync(
                    new IngestPayloadCommand(
                        sourceCode,
                        file.FileName,
                        format,
                        channel,
                        buffer.ToArray(),
                        reprocess,
                        idempotencyKey),
                    cancellationToken);

                return result.ToCreatedResult(httpContext, batch => $"/api/v1/batches/{batch.Id}");
            })
            .RequireAuthorization(Permissions.Ingestion.Write)
            .DisableAntiforgery()
            .WithName("UploadBatch")
            .WithSummary("Ingests an ISO 20022 XML or delimited payload; unsafe files are quarantined.")
            .Produces<IngestionBatchDto>(StatusCodes.Status201Created);

        batches.MapPost("/{batchId:guid}/retry", async (
                Guid batchId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new RetryBatchCommand(batchId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Ingestion.Manage)
            .WithName("RetryBatch")
            .WithSummary("Re-parses a failed batch from its stored payload.")
            .Produces<IngestionBatchDto>();

        batches.MapPost("/{batchId:guid}/cancel", async (
                Guid batchId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new CancelBatchCommand(batchId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Ingestion.Manage)
            .WithName("CancelBatch");

        // Service-to-service route consumed by validation. It is deliberately not published by the
        // gateway, and returns unmasked addresses to a caller entitled to run validation.
        var internalBatches = app.MapGroup("/internal/v1/batches").WithTags("Internal").ExcludeFromDescription();

        internalBatches.MapGet("/{batchId:guid}", async (
                Guid batchId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetBatchByIdQuery(batchId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Validation.Run)
            .WithName("GetBatchForValidation");

        internalBatches.MapGet("/{batchId:guid}/records", async (
                Guid batchId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new ExportBatchRecordsQuery(batchId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Validation.Run)
            .WithName("ExportBatchRecords");

        return app;
    }
}
