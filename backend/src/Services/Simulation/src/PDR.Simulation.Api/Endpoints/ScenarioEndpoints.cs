using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.Simulation.Application.Scenarios;
using PDR.Simulation.Domain.Scenarios;

namespace PDR.Simulation.Api.Endpoints;

public static class ScenarioEndpoints
{
    public static IEndpointRouteBuilder MapScenarioEndpoints(this IEndpointRouteBuilder app)
    {
        var scenarios = app.MapGroup("/api/v1/simulation/scenarios").WithTags("Scenarios");

        scenarios.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetScenariosQuery(), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Simulation.Read)
            .WithName("GetScenarios")
            .WithSummary("Scenario definitions with their run history.")
            .Produces<IReadOnlyList<ScenarioDto>>();

        scenarios.MapGet("/{code}", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetScenarioQuery(code), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Simulation.Read)
            .WithName("GetScenario")
            .Produces<ScenarioDto>();

        scenarios.MapPost("/", async (
                HttpContext httpContext,
                ISender sender,
                CreateScenarioRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new CreateScenarioCommand(
                        request.Code,
                        request.Name,
                        request.Mode,
                        request.AsOf,
                        request.SchemeCodes,
                        request.SourceCodes,
                        request.Countries,
                        request.PartyRoles,
                        request.Exclusions,
                        request.RulesetVersion,
                        request.Description),
                    cancellationToken);

                return result.ToCreatedResult(
                    httpContext,
                    scenario => $"/api/v1/simulation/scenarios/{scenario.Code}");
            })
            .RequireAuthorization(Permissions.Simulation.Write)
            .WithName("CreateScenario")
            .WithSummary("Defines a current, future or remediated scenario with its scope filters.")
            .Produces<ScenarioDto>(StatusCodes.Status201Created);

        scenarios.MapPut("/{code}", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                UpdateScenarioRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new UpdateScenarioCommand(
                        code,
                        request.Name,
                        request.AsOf,
                        request.SchemeCodes,
                        request.SourceCodes,
                        request.Countries,
                        request.PartyRoles,
                        request.Exclusions,
                        request.RulesetVersion,
                        request.Description),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Simulation.Write)
            .WithName("UpdateScenario")
            .Produces<ScenarioDto>();

        scenarios.MapPost("/{code}/lock", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new LockScenarioCommand(code), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Simulation.Write)
            .WithName("LockScenario")
            .WithSummary("Freezes a scenario so its runs stay comparable.")
            .Produces<ScenarioDto>();

        scenarios.MapPost("/{code}/archive", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new ArchiveScenarioCommand(code), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Simulation.Write)
            .WithName("ArchiveScenario")
            .Produces<ScenarioDto>();

        scenarios.MapPost("/{code}/run", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new RunScenarioCommand(code), cancellationToken);
                return result.ToCreatedResult(httpContext, run => $"/api/v1/simulation/runs/{run.Id}");
            })
            .RequireAuthorization(Permissions.Simulation.Write)
            .WithName("RunScenario")
            .WithSummary("Executes the scenario and stores a reproducible, comparable run.")
            .Produces<SimulationRunDto>(StatusCodes.Status201Created);

        var runs = app.MapGroup("/api/v1/simulation/runs").WithTags("Simulation runs");

        runs.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                string? scenarioCode,
                int? page,
                int? pageSize,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new GetRunsQuery(scenarioCode, page ?? 1, pageSize),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Simulation.Read)
            .WithName("GetSimulationRuns")
            .Produces<PagedResult<SimulationRunDto>>();

        runs.MapGet("/{id:guid}", async (
                Guid id,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetRunQuery(id), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Simulation.Read)
            .WithName("GetSimulationRun")
            .WithSummary("A stored run with its dimensional breakdown.")
            .Produces<SimulationRunDto>();

        runs.MapGet("/compare", async (
                HttpContext httpContext,
                ISender sender,
                Guid baselineId,
                Guid candidateId,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new CompareRunsQuery(baselineId, candidateId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Simulation.Read)
            .WithName("CompareSimulationRuns")
            .WithSummary("Deltas between two completed runs, per dimension.")
            .Produces<RunComparisonDto>();

        return app;
    }

    public sealed record CreateScenarioRequest(
        string Code,
        string Name,
        ScenarioMode Mode,
        DateOnly AsOf,
        string? SchemeCodes,
        string? SourceCodes,
        string? Countries,
        string? PartyRoles,
        string? Exclusions,
        string? RulesetVersion,
        string? Description);

    public sealed record UpdateScenarioRequest(
        string Name,
        DateOnly AsOf,
        string? SchemeCodes,
        string? SourceCodes,
        string? Countries,
        string? PartyRoles,
        string? Exclusions,
        string? RulesetVersion,
        string? Description);
}
