using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.Sources.Application.Inventory;
using PDR.Sources.Application.Inventory.Commands;
using PDR.Sources.Application.Inventory.Queries;
using PDR.Sources.Domain.Inventory;

namespace PDR.Sources.Api.Endpoints;

public sealed record RegisterSourceRequest(
    string Code,
    string Name,
    SourceKind Kind,
    InterfaceKind Interface,
    string OwnerName,
    string OwnerEmail,
    string LegalEntity,
    IReadOnlyList<string> SchemeCodes,
    string? Schedule,
    long EstimatedPartyCount,
    long RecurringInstructionCount,
    bool IsAuthoritative);

public sealed record UpdateSourceRequest(
    string Name,
    SourceKind Kind,
    InterfaceKind Interface,
    string OwnerName,
    string OwnerEmail,
    string LegalEntity,
    IReadOnlyList<string> SchemeCodes,
    string? Schedule,
    long EstimatedPartyCount,
    long RecurringInstructionCount,
    bool IsAuthoritative,
    OnboardingStatus Status,
    MappingReadiness Mapping,
    string? RemediationOwner,
    bool IsActive);

public sealed record ReplaceLineageRequest(IReadOnlyList<LineageStepInput> Steps);

public sealed record RecordScanRequest(decimal CoveragePercent);

public static class SourcesEndpoints
{
    public static IEndpointRouteBuilder MapSourcesEndpoints(this IEndpointRouteBuilder app)
    {
        var sources = app.MapGroup("/api/v1/sources").WithTags("Sources");

        sources.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                string? schemeCode = null,
                OnboardingStatus? status = null,
                string? legalEntity = null,
                bool attestationOverdueOnly = false,
                bool includeInactive = false,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(
                    new GetSourcesQuery(schemeCode, status, legalEntity, attestationOverdueOnly, includeInactive),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Sources.Read)
            .WithName("GetSources")
            .WithSummary("Registered source systems with mapping, scan and attestation state.")
            .Produces<IReadOnlyList<SourceSystemDto>>();

        sources.MapGet("/readiness", async (
                HttpContext httpContext,
                ISender sender,
                string? schemeCode = null,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(new GetSourceReadinessQuery(schemeCode), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Sources.Read)
            .WithName("GetSourceReadiness")
            .WithSummary("Portfolio-level onboarding readiness across registered sources.")
            .Produces<SourceReadinessSummaryDto>();

        sources.MapGet("/{code}", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetSourceByCodeQuery(code), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Sources.Read)
            .WithName("GetSourceByCode")
            .Produces<SourceSystemDto>();

        sources.MapPost("/", async (
                RegisterSourceRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new RegisterSourceCommand(
                        request.Code,
                        request.Name,
                        request.Kind,
                        request.Interface,
                        request.OwnerName,
                        request.OwnerEmail,
                        request.LegalEntity,
                        request.SchemeCodes,
                        request.Schedule,
                        request.EstimatedPartyCount,
                        request.RecurringInstructionCount,
                        request.IsAuthoritative),
                    cancellationToken);

                return result.ToCreatedResult(httpContext, _ => $"/api/v1/sources/{request.Code.ToUpperInvariant()}");
            })
            .RequireAuthorization(Permissions.Sources.Write)
            .WithName("RegisterSource");

        sources.MapPut("/{code}", async (
                string code,
                UpdateSourceRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new UpdateSourceCommand(
                        code,
                        request.Name,
                        request.Kind,
                        request.Interface,
                        request.OwnerName,
                        request.OwnerEmail,
                        request.LegalEntity,
                        request.SchemeCodes,
                        request.Schedule,
                        request.EstimatedPartyCount,
                        request.RecurringInstructionCount,
                        request.IsAuthoritative,
                        request.Status,
                        request.Mapping,
                        request.RemediationOwner,
                        request.IsActive),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Sources.Write)
            .WithName("UpdateSource");

        sources.MapPost("/{code}/mappings", async (
                string code,
                FieldMappingInput request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new AddFieldMappingCommand(code, request), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Sources.Write)
            .WithName("AddFieldMapping")
            .WithSummary("Maps a source attribute to the ISO 20022 address element it feeds.");

        sources.MapDelete("/{code}/mappings/{mappingId:guid}", async (
                string code,
                Guid mappingId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new RemoveFieldMappingCommand(code, mappingId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Sources.Write)
            .WithName("RemoveFieldMapping");

        sources.MapPut("/{code}/lineage", async (
                string code,
                ReplaceLineageRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new ReplaceLineageCommand(code, request.Steps), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Sources.Write)
            .WithName("ReplaceLineage")
            .WithSummary("Replaces the lineage path from authoritative record to submitted message.");

        sources.MapPost("/{code}/scan", async (
                string code,
                RecordScanRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new RecordScanCommand(code, request.CoveragePercent),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Sources.Write)
            .WithName("RecordSourceScan");

        sources.MapPost("/{code}/attestation", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new AttestSourceCommand(code), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Sources.Attest)
            .WithName("AttestSource")
            .WithSummary("Owner attestation that the inventory and field mappings are still correct.");

        return app;
    }
}
