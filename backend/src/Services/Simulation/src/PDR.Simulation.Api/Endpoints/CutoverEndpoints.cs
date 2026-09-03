using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.Simulation.Application.Cutover;
using PDR.Simulation.Domain.Cutover;

namespace PDR.Simulation.Api.Endpoints;

public static class CutoverEndpoints
{
    public static IEndpointRouteBuilder MapCutoverEndpoints(this IEndpointRouteBuilder app)
    {
        var plans = app.MapGroup("/api/v1/simulation/cutover").WithTags("Cutover");

        plans.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetCutoverPlansQuery(), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Cutover.Read)
            .WithName("GetCutoverPlans")
            .Produces<IReadOnlyList<CutoverPlanDto>>();

        plans.MapGet("/{code}", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetCutoverPlanQuery(code), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Cutover.Read)
            .WithName("GetCutoverPlan")
            .Produces<CutoverPlanDto>();

        plans.MapGet("/{code}/go-no-go", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetGoNoGoPackQuery(code), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Cutover.Read)
            .WithName("GetGoNoGoPack")
            .WithSummary("Residual exposure, exceptions, testing and readiness with an evidence-derived recommendation.")
            .Produces<GoNoGoPackDto>();

        plans.MapPost("/", async (
                HttpContext httpContext,
                ISender sender,
                CreateCutoverPlanRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new CreateCutoverPlanCommand(request.Code, request.Name, request.CutoverDate, request.Owner),
                    cancellationToken);

                return result.ToCreatedResult(httpContext, plan => $"/api/v1/simulation/cutover/{plan.Code}");
            })
            .RequireAuthorization(Permissions.Cutover.Write)
            .WithName("CreateCutoverPlan")
            .Produces<CutoverPlanDto>(StatusCodes.Status201Created);

        plans.MapPut("/{code}/operations", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                SetOperationalPlanRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new SetOperationalPlanCommand(
                        code,
                        request.FreezeFrom,
                        request.FreezeTo,
                        request.FallbackPlan,
                        request.SupportModel),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Cutover.Write)
            .WithName("SetCutoverOperationalPlan")
            .WithSummary("Sets the change freeze window, the fallback plan and the support model.")
            .Produces<CutoverPlanDto>();

        plans.MapPost("/{code}/criteria", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                AddCriterionRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new AddCriterionCommand(
                        code,
                        request.Reference,
                        request.Kind,
                        request.Description,
                        request.Owner,
                        request.IsBlocking),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Cutover.Write)
            .WithName("AddCutoverCriterion")
            .Produces<CutoverPlanDto>();

        plans.MapPost("/{code}/criteria/{reference}/status", async (
                string code,
                string reference,
                HttpContext httpContext,
                ISender sender,
                RecordCriterionRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new RecordCriterionCommand(
                        code,
                        reference,
                        request.Status,
                        request.EvidenceReference,
                        request.Rationale),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Cutover.Write)
            .WithName("RecordCutoverCriterion")
            .WithSummary("Marks a criterion met (evidence required) or waived (rationale required).")
            .Produces<CutoverPlanDto>();

        plans.MapPost("/{code}/approvals", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                ApproveCutoverRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new ApproveCutoverCommand(code, request.Role, request.Decision, request.Rationale),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Cutover.Approve)
            .WithName("ApproveCutover")
            .WithSummary("Records a sign-off stamped with the recommendation shown at the time.")
            .Produces<CutoverPlanDto>();

        return app;
    }

    public sealed record CreateCutoverPlanRequest(string Code, string Name, DateOnly CutoverDate, string Owner);

    public sealed record SetOperationalPlanRequest(
        DateOnly? FreezeFrom,
        DateOnly? FreezeTo,
        string? FallbackPlan,
        string? SupportModel);

    public sealed record AddCriterionRequest(
        string Reference,
        CriterionKind Kind,
        string Description,
        string Owner,
        bool IsBlocking);

    public sealed record RecordCriterionRequest(CriterionStatus Status, string? EvidenceReference, string? Rationale);

    public sealed record ApproveCutoverRequest(string Role, ApprovalDecision Decision, string Rationale);
}
