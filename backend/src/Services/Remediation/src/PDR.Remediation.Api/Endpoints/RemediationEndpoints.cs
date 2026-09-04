using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.Remediation.Application.Cases;
using PDR.Remediation.Application.Cases.Commands;
using PDR.Remediation.Application.Cases.Queries;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Api.Endpoints;

public static class RemediationEndpoints
{
    public static IEndpointRouteBuilder MapRemediationEndpoints(this IEndpointRouteBuilder app)
    {
        var cases = app.MapGroup("/api/v1/remediation/cases").WithTags("Remediation");

        cases.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                int page = 1,
                int? pageSize = null,
                CaseStatus? status = null,
                CasePriority? priority = null,
                string? sourceCode = null,
                string? queue = null,
                string? assignedTo = null,
                string? ruleCode = null,
                Guid? campaignId = null,
                bool overdueOnly = false,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(
                    new GetCasesQuery(
                        page,
                        pageSize,
                        status,
                        priority,
                        sourceCode,
                        queue,
                        assignedTo,
                        ruleCode,
                        campaignId,
                        overdueOnly),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Read)
            .WithName("GetRemediationCases")
            .WithSummary("The remediation queue, ordered by priority and due date.")
            .Produces<PagedResult<CaseListItemDto>>();

        cases.MapGet("/{caseId:guid}", async (
                Guid caseId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetCaseByIdQuery(caseId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Read)
            .WithName("GetRemediationCaseById")
            .WithSummary("One case with its proposal, evidence and full decision history.")
            .Produces<CaseDetailDto>();

        cases.MapPost("/generate", async (
                HttpContext httpContext,
                ISender sender,
                GenerateCasesRequest? request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GenerateCasesCommand(request?.RunId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Write)
            .WithName("GenerateRemediationCases")
            .WithSummary("Opens one case per defective party address in a validation run, folding repeats into occurrences.")
            .Produces<CaseGenerationDto>();

        cases.MapPut("/{caseId:guid}/proposal", async (
                Guid caseId,
                HttpContext httpContext,
                ISender sender,
                ProposeCorrectionRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new ProposeCorrectionCommand(
                        caseId,
                        request.Country,
                        request.TownName,
                        request.PostCode,
                        request.StreetName,
                        request.BuildingNumber,
                        request.Notes),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Write)
            .WithName("ProposeCorrection")
            .WithSummary("Replaces the proposed structured address with the maker's own values.")
            .Produces<CaseDetailDto>();

        cases.MapPost("/{caseId:guid}/evidence", async (
                Guid caseId,
                HttpContext httpContext,
                ISender sender,
                AddEvidenceRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new AddCaseEvidenceCommand(caseId, request.Kind, request.Reference, request.Description),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Write)
            .WithName("AddCaseEvidence")
            .Produces<CaseDetailDto>();

        cases.MapPost("/{caseId:guid}/assign", async (
                Guid caseId,
                HttpContext httpContext,
                ISender sender,
                AssignCaseRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new AssignCaseCommand(caseId, request.Queue, request.AssignedTo, request.DueDate),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Write)
            .WithName("AssignRemediationCase")
            .Produces<CaseDetailDto>();

        cases.MapPost("/{caseId:guid}/submit", async (
                Guid caseId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new SubmitCaseCommand(caseId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Write)
            .WithName("SubmitRemediationCase")
            .WithSummary("Sends the maker's proposal for independent approval.")
            .Produces<CaseDetailDto>();

        cases.MapPost("/{caseId:guid}/decision", async (
                Guid caseId,
                HttpContext httpContext,
                ISender sender,
                DecideCaseRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new DecideCaseCommand(caseId, request.Decision, request.Rationale, request.ExceptionExpiresOn),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Approve)
            .WithName("DecideRemediationCase")
            .WithSummary("The checker approves, returns, rejects, dismisses or grants a time-bound exception.")
            .Produces<CaseDetailDto>();

        var bulk = app.MapGroup("/api/v1/remediation/bulk").WithTags("Remediation");

        bulk.MapPost("/preview", async (
                HttpContext httpContext,
                ISender sender,
                BulkActionRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new PreviewBulkActionCommand(request.Action, request.Selection),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Read)
            .WithName("PreviewBulkAction")
            .WithSummary("Counts, exposure, blockers and rollback scope of a bulk action before it runs.")
            .Produces<BulkPreviewDto>();

        bulk.MapPost("/apply", async (
                HttpContext httpContext,
                ISender sender,
                BulkActionRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new ApplyBulkActionCommand(request.Action, request.Selection, request.Rationale),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Approve)
            .WithName("ApplyBulkAction")
            .Produces<BulkResultDto>();

        app.MapGet("/api/v1/remediation/funnel", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetFunnelQuery(), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Read)
            .WithTags("Remediation")
            .WithName("GetRemediationFunnel")
            .WithSummary("Case funnel with exceptions, expiries, overdue work and exposure by source.")
            .Produces<RemediationFunnelDto>();

        return app;
    }
}

public sealed record GenerateCasesRequest(Guid? RunId);

public sealed record ProposeCorrectionRequest(
    string? Country,
    string? TownName,
    string? PostCode,
    string? StreetName,
    string? BuildingNumber,
    string? Notes);

public sealed record AddEvidenceRequest(string Kind, string Reference, string? Description);

public sealed record AssignCaseRequest(string Queue, string? AssignedTo, DateOnly? DueDate);

public sealed record DecideCaseRequest(DecisionType Decision, string? Rationale, DateOnly? ExceptionExpiresOn);

public sealed record BulkActionRequest(string Action, BulkSelection Selection, string? Rationale);
