using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.Simulation.Application.Testing;
using PDR.Simulation.Domain.Testing;

namespace PDR.Simulation.Api.Endpoints;

public static class TestingEndpoints
{
    public static IEndpointRouteBuilder MapTestingEndpoints(this IEndpointRouteBuilder app)
    {
        var plans = app.MapGroup("/api/v1/simulation/test-plans").WithTags("Test plans");

        plans.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetTestPlansQuery(), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Testing.Read)
            .WithName("GetTestPlans")
            .WithSummary("Risk-based test plans with execution and defect counts.")
            .Produces<IReadOnlyList<TestPlanDto>>();

        plans.MapGet("/{code}", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetTestPlanQuery(code), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Testing.Read)
            .WithName("GetTestPlan")
            .Produces<TestPlanDto>();

        plans.MapPost("/", async (
                HttpContext httpContext,
                ISender sender,
                CreateTestPlanRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new CreateTestPlanCommand(request.Code, request.Name, request.Owner, request.Scope, request.Description),
                    cancellationToken);

                return result.ToCreatedResult(httpContext, plan => $"/api/v1/simulation/test-plans/{plan.Code}");
            })
            .RequireAuthorization(Permissions.Testing.Write)
            .WithName("CreateTestPlan")
            .Produces<TestPlanDto>(StatusCodes.Status201Created);

        plans.MapPost("/{code}/cases", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                AddTestCaseRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new AddTestCaseCommand(
                        code,
                        request.Reference,
                        request.Title,
                        request.Risk,
                        request.ScenarioCode,
                        request.SampleReference,
                        request.ExpectedResult),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Testing.Write)
            .WithName("AddTestCase")
            .WithSummary("Adds a risk-weighted case, optionally bound to a scenario and a sample.")
            .Produces<TestPlanDto>();

        plans.MapPost("/{code}/activate", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new ActivateTestPlanCommand(code), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Testing.Write)
            .WithName("ActivateTestPlan")
            .Produces<TestPlanDto>();

        plans.MapPost("/{code}/close", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new CloseTestPlanCommand(code), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Testing.Write)
            .WithName("CloseTestPlan")
            .WithSummary("Closes a plan; refused while defects are still open.")
            .Produces<TestPlanDto>();

        plans.MapPost("/{code}/cases/{reference}/execution", async (
                string code,
                string reference,
                HttpContext httpContext,
                ISender sender,
                RecordExecutionRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new RecordExecutionCommand(
                        code,
                        reference,
                        request.Status,
                        request.ActualResult,
                        request.EvidenceReference,
                        request.DefectReference),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Testing.Write)
            .WithName("RecordTestExecution")
            .WithSummary("Records an execution or retest; a failure must carry a defect reference.")
            .Produces<TestPlanDto>();

        plans.MapPost("/{code}/cases/{reference}/uat", async (
                string code,
                string reference,
                HttpContext httpContext,
                ISender sender,
                RecordUatRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new RecordUatOutcomeCommand(
                        code,
                        reference,
                        request.EngineOutcome,
                        request.PlatformOutcome,
                        request.Explanation),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Testing.Write)
            .WithName("RecordUatOutcome")
            .WithSummary("Reconciles the payment engine or network outcome against the platform prediction.")
            .Produces<TestPlanDto>();

        return app;
    }

    public sealed record CreateTestPlanRequest(string Code, string Name, string Owner, string? Scope, string? Description);

    public sealed record AddTestCaseRequest(
        string Reference,
        string Title,
        TestRisk Risk,
        string? ScenarioCode,
        string? SampleReference,
        string ExpectedResult);

    public sealed record RecordExecutionRequest(
        TestExecutionStatus Status,
        string ActualResult,
        string? EvidenceReference,
        string? DefectReference);

    public sealed record RecordUatRequest(string EngineOutcome, string PlatformOutcome, string? Explanation);
}
