using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.Rules.Application.Rulesets;
using PDR.Rules.Application.Rulesets.Commands;
using PDR.Rules.Application.Rulesets.Queries;
using PDR.Rules.Domain.Rulesets;

namespace PDR.Rules.Api.Endpoints;

public sealed record CreateSchemeRequest(
    string Code,
    string Name,
    string? Description,
    DateOnly? StructuredAddressMandatoryFrom);

public sealed record UpdateSchemeRequest(
    string Name,
    string? Description,
    DateOnly? StructuredAddressMandatoryFrom,
    bool IsActive);

public sealed record CreateRulesetRequest(string SchemeCode, string Name, string? Description);

public sealed record AddVersionRequest(int? CopyFromVersionNumber, string? Notes);

public sealed record ActivateVersionRequest(DateOnly EffectiveFrom);

public static class RulesEndpoints
{
    public static IEndpointRouteBuilder MapRulesEndpoints(this IEndpointRouteBuilder app)
    {
        MapSchemes(app);
        MapRulesets(app);
        return app;
    }

    private static void MapSchemes(IEndpointRouteBuilder app)
    {
        var schemes = app.MapGroup("/api/v1/schemes").WithTags("Schemes");

        schemes.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                bool includeInactive = false,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(new GetSchemesQuery(includeInactive), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Rules.Read)
            .WithName("GetSchemes")
            .WithSummary("Payment schemes in scope and their structured-address cutover dates.")
            .Produces<IReadOnlyList<SchemeDto>>();

        schemes.MapPost("/", async (
                CreateSchemeRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new CreateSchemeCommand(
                        request.Code,
                        request.Name,
                        request.Description,
                        request.StructuredAddressMandatoryFrom),
                    cancellationToken);

                return result.ToCreatedResult(httpContext, _ => $"/api/v1/schemes/{request.Code}");
            })
            .RequireAuthorization(Permissions.Rules.Write)
            .WithName("CreateScheme");

        schemes.MapPut("/{code}", async (
                string code,
                UpdateSchemeRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new UpdateSchemeCommand(
                        code,
                        request.Name,
                        request.Description,
                        request.StructuredAddressMandatoryFrom,
                        request.IsActive),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Rules.Write)
            .WithName("UpdateScheme");

        app.MapGet("/api/v1/countries", async (
                HttpContext httpContext,
                ISender sender,
                bool sepaOnly = false,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(new GetCountriesQuery(sepaOnly), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Rules.Read)
            .WithTags("Reference data")
            .WithName("GetCountries")
            .Produces<IReadOnlyList<CountryDto>>();
    }

    private static void MapRulesets(IEndpointRouteBuilder app)
    {
        var rulesets = app.MapGroup("/api/v1/rulesets").WithTags("Rulesets");

        rulesets.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                string? schemeCode = null,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(new GetRulesetsQuery(schemeCode), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Rules.Read)
            .WithName("GetRulesets")
            .Produces<IReadOnlyList<RulesetDto>>();

        rulesets.MapGet("/{id:guid}", async (
                Guid id,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetRulesetByIdQuery(id), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Rules.Read)
            .WithName("GetRulesetById")
            .Produces<RulesetDto>();

        rulesets.MapGet("/effective", async (
                HttpContext httpContext,
                ISender sender,
                string schemeCode,
                DateOnly? asOf = null,
                RuleApplicability mode = RuleApplicability.Current,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(
                    new GetEffectiveRulesQuery(schemeCode, asOf, mode),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Rules.Read)
            .WithName("GetEffectiveRules")
            .WithSummary("Rules a scheme enforces on a date, for current or post-cutover validation.")
            .Produces<EffectiveRulesetDto>();

        rulesets.MapPost("/", async (
                CreateRulesetRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new CreateRulesetCommand(request.SchemeCode, request.Name, request.Description),
                    cancellationToken);

                return result.ToCreatedResult(httpContext, id => $"/api/v1/rulesets/{id}");
            })
            .RequireAuthorization(Permissions.Rules.Write)
            .WithName("CreateRuleset");

        rulesets.MapPost("/{id:guid}/versions", async (
                Guid id,
                AddVersionRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new AddRulesetVersionCommand(id, request.CopyFromVersionNumber, request.Notes),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Rules.Write)
            .WithName("AddRulesetVersion")
            .WithSummary("Creates a new draft version, optionally copying the rules of an existing one.");

        rulesets.MapPost("/{id:guid}/versions/{versionNumber:int}/rules", async (
                Guid id,
                int versionNumber,
                RuleInput rule,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new AddRuleCommand(id, versionNumber, rule), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Rules.Write)
            .WithName("AddRule");

        rulesets.MapDelete("/{id:guid}/versions/{versionNumber:int}/rules/{ruleId:guid}", async (
                Guid id,
                int versionNumber,
                Guid ruleId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new RemoveRuleCommand(id, versionNumber, ruleId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Rules.Write)
            .WithName("RemoveRule");

        rulesets.MapPost("/{id:guid}/versions/{versionNumber:int}/activate", async (
                Guid id,
                int versionNumber,
                ActivateVersionRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new ActivateRulesetVersionCommand(id, versionNumber, request.EffectiveFrom),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Rules.Activate)
            .WithName("ActivateRulesetVersion")
            .WithSummary("Activates a version from a date; re-activating an earlier version is the rollback path.");
    }
}
