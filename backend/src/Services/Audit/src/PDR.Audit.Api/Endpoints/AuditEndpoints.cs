using PDR.Audit.Application.Ledger;
using PDR.Audit.Application.Ledger.Commands;
using PDR.Audit.Application.Ledger.Queries;
using PDR.Audit.Domain.Ledger;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;

namespace PDR.Audit.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var audit = app.MapGroup("/api/v1/audit").WithTags("Audit");

        audit.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                int page = 1,
                int pageSize = 20,
                string? service = null,
                string? action = null,
                string? entityType = null,
                string? entityId = null,
                string? actor = null,
                string? correlationId = null,
                AuditOutcome? outcome = null,
                DateTimeOffset? from = null,
                DateTimeOffset? to = null,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(
                    new GetAuditRecordsQuery(
                        page,
                        pageSize,
                        service,
                        action,
                        entityType,
                        entityId,
                        actor,
                        correlationId,
                        outcome,
                        from,
                        to),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Audit.Read)
            .WithName("GetAuditRecords")
            .WithSummary("Searchable audit trail, newest first.")
            .Produces<PagedResult<AuditRecordDto>>();

        audit.MapGet("/{id:guid}", async (
                Guid id,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetAuditRecordByIdQuery(id), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Audit.Read)
            .WithName("GetAuditRecordById")
            .Produces<AuditRecordDto>();

        audit.MapPost("/", async (
                AppendAuditRecordCommand command,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(command, cancellationToken);
                return result.ToCreatedResult(httpContext, record => $"/api/v1/audit/{record.Id}");
            })
            .RequireAuthorization(Permissions.Audit.Write)
            .WithName("AppendAuditRecord")
            .WithSummary("Appends a record to the tamper-evident ledger; records are never updated or deleted.");

        audit.MapGet("/verify", async (
                HttpContext httpContext,
                ISender sender,
                long? fromSequence = null,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(new VerifyAuditChainQuery(fromSequence), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Audit.Verify)
            .WithName("VerifyAuditChain")
            .WithSummary("Re-hashes the ledger and reports the first record whose hash no longer reproduces.")
            .Produces<AuditChainVerificationDto>();

        return app;
    }
}
