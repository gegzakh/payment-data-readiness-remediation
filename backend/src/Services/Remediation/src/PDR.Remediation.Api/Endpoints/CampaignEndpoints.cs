using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.Remediation.Application.Campaigns;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Api.Endpoints;

public static class CampaignEndpoints
{
    public static IEndpointRouteBuilder MapCampaignEndpoints(this IEndpointRouteBuilder app)
    {
        var campaigns = app.MapGroup("/api/v1/remediation/campaigns").WithTags("Campaigns");

        campaigns.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetCampaignsQuery(), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Read)
            .WithName("GetCampaigns")
            .WithSummary("Remediation campaigns with progress derived from their cases.")
            .Produces<IReadOnlyList<CampaignDto>>();

        campaigns.MapPost("/", async (
                HttpContext httpContext,
                ISender sender,
                CreateCampaignRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new CreateCampaignCommand(
                        request.Code,
                        request.Name,
                        request.Audience,
                        request.Assignee,
                        request.DueDate,
                        request.Description),
                    cancellationToken);

                return result.ToCreatedResult(
                    httpContext,
                    campaign => $"/api/v1/remediation/campaigns/{campaign.Code}");
            })
            .RequireAuthorization(Permissions.Remediation.Write)
            .WithName("CreateCampaign")
            .Produces<CampaignDto>(StatusCodes.Status201Created);

        campaigns.MapPost("/{code}/cases", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                AssignCasesRequest request,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new AssignCasesToCampaignCommand(code, request.CaseIds),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Remediation.Write)
            .WithName("AssignCasesToCampaign")
            .WithSummary("Adds cases to a campaign and activates it.")
            .Produces<CampaignDto>();

        return app;
    }
}

public sealed record CreateCampaignRequest(
    string Code,
    string Name,
    CampaignAudience Audience,
    string Assignee,
    DateOnly DueDate,
    string? Description);

public sealed record AssignCasesRequest(IReadOnlyList<Guid> CaseIds);
