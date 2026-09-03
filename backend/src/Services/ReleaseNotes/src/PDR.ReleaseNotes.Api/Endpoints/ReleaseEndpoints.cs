using Microsoft.AspNetCore.Http.HttpResults;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.ReleaseNotes.Application.Releases;
using PDR.ReleaseNotes.Application.Releases.Commands;
using PDR.ReleaseNotes.Application.Releases.Queries;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.Api.Endpoints;

public sealed record CreateReleaseRequest(
    string Version,
    string Title,
    DateOnly ReleaseDate,
    string? Summary,
    IReadOnlyList<ReleaseEntryInput>? Entries);

public sealed record UpdateReleaseRequest(
    string Version,
    string Title,
    DateOnly ReleaseDate,
    string? Summary);

public sealed record AddErratumRequest(
    string Component,
    string Title,
    string? Body,
    IReadOnlyList<string>? References);

public static class ReleaseEndpoints
{
    public static IEndpointRouteBuilder MapReleaseEndpoints(this IEndpointRouteBuilder app)
    {
        var releases = app.MapGroup("/api/v1/releases").WithTags("Releases");

        releases.MapGet("/", async (
                HttpContext httpContext,
                ISender sender,
                int page = 1,
                int? pageSize = null,
                ReleaseEntryType? type = null,
                string? component = null,
                DateOnly? from = null,
                DateOnly? to = null,
                bool includeDrafts = false,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.SendAsync(
                    new GetReleasesQuery(page, pageSize, type, component, from, to, includeDrafts),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .AllowAnonymous()
            .WithName("GetReleases")
            .WithSummary("Paged release notes, newest release first.")
            .Produces<PagedResult<ReleaseDto>>();

        releases.MapGet("/page-sizes", async (PageSizeResolver resolver, CancellationToken cancellationToken) =>
                Results.Ok(await resolver.GetAllowedPageSizesAsync(cancellationToken)))
            .AllowAnonymous()
            .WithName("GetReleasePageSizes")
            .WithSummary("Page sizes the release-notes page may offer (runtime configurable).");

        releases.MapGet("/{id:guid}", async (
                Guid id,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetReleaseByIdQuery(id), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .AllowAnonymous()
            .WithName("GetReleaseById")
            .Produces<ReleaseDto>();

        var admin = app.MapGroup("/api/v1/admin/releases").WithTags("Releases (admin)");

        admin.MapPost("/", async (
                CreateReleaseRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new CreateReleaseCommand(
                        request.Version,
                        request.Title,
                        request.ReleaseDate,
                        request.Summary,
                        request.Entries),
                    cancellationToken);

                return result.ToCreatedResult(httpContext, id => $"/api/v1/releases/{id}");
            })
            .RequireAuthorization(Permissions.ReleaseNotes.Write)
            .WithName("CreateRelease");

        admin.MapPut("/{id:guid}", async (
                Guid id,
                UpdateReleaseRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new UpdateReleaseCommand(id, request.Version, request.Title, request.ReleaseDate, request.Summary),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.ReleaseNotes.Write)
            .WithName("UpdateRelease");

        admin.MapPost("/{id:guid}/entries", async (
                Guid id,
                ReleaseEntryInput entry,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new AddReleaseEntryCommand(id, entry), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.ReleaseNotes.Write)
            .WithName("AddReleaseEntry");

        admin.MapPut("/{id:guid}/entries/{entryId:guid}", async (
                Guid id,
                Guid entryId,
                ReleaseEntryInput entry,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new UpdateReleaseEntryCommand(id, entryId, entry),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.ReleaseNotes.Write)
            .WithName("UpdateReleaseEntry");

        admin.MapDelete("/{id:guid}/entries/{entryId:guid}", async (
                Guid id,
                Guid entryId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new RemoveReleaseEntryCommand(id, entryId), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.ReleaseNotes.Write)
            .WithName("RemoveReleaseEntry");

        admin.MapPost("/{id:guid}/publish", async (
                Guid id,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new PublishReleaseCommand(id), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.ReleaseNotes.Publish)
            .WithName("PublishRelease");

        admin.MapPost("/{id:guid}/errata", async (
                Guid id,
                AddErratumRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new AddErratumCommand(id, request.Component, request.Title, request.Body, request.References),
                    cancellationToken);

                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.ReleaseNotes.Publish)
            .WithName("AddReleaseErratum");

        return app;
    }
}
