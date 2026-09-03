using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.ReleaseNotes.Application.Abstractions;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.Application.Releases.Queries;

/// <summary>
/// Paged release feed, newest release first. Drafts are only returned to authors.
/// </summary>
public sealed record GetReleasesQuery(
    int Page = 1,
    int? PageSize = null,
    ReleaseEntryType? Type = null,
    string? Component = null,
    DateOnly? From = null,
    DateOnly? To = null,
    bool IncludeDrafts = false) : IQuery<PagedResult<ReleaseDto>>;

public sealed class GetReleasesQueryValidator : AbstractValidator<GetReleasesQuery>
{
    public GetReleasesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).GreaterThan(0).When(query => query.PageSize.HasValue);
        RuleFor(query => query.Component).MaximumLength(128);
        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From!.Value)
            .When(query => query.From.HasValue && query.To.HasValue)
            .WithMessage("'To' must not be earlier than 'From'.");
    }
}

public sealed class GetReleasesQueryHandler(
    IReleaseNotesDbContext context,
    PageSizeResolver pageSizeResolver,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<GetReleasesQuery, Result<PagedResult<ReleaseDto>>>
{
    public async Task<Result<PagedResult<ReleaseDto>>> HandleAsync(
        GetReleasesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.IncludeDrafts && !currentUser.HasPermission(Permissions.ReleaseNotes.Write))
        {
            return Result.Failure<PagedResult<ReleaseDto>>(
                Error.Forbidden(
                    "RELEASE.DRAFTS_FORBIDDEN",
                    "Draft releases require the releasenotes.write permission."));
        }

        var pageSize = await pageSizeResolver.ResolveAsync(request.PageSize, cancellationToken);

        var query = context.Releases.AsNoTracking().AsQueryable();

        if (!request.IncludeDrafts)
        {
            query = query.Where(release => release.Status == ReleaseStatus.Published);
        }

        if (request.From.HasValue)
        {
            query = query.Where(release => release.ReleaseDate >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(release => release.ReleaseDate <= request.To.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(release => release.Entries.Any(entry => entry.Type == request.Type.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.Component))
        {
            var component = request.Component.ToLowerInvariant();
#pragma warning disable CA1862 // StringComparison overloads cannot be translated to SQL by EF Core.
            query = query.Where(release =>
                release.Entries.Any(entry => entry.Component.ToLower() == component));
#pragma warning restore CA1862
        }

        var totalCount = await query.LongCountAsync(cancellationToken);

        var releases = await query
            .OrderByDescending(release => release.ReleaseDate)
            .ThenByDescending(release => release.Version)
            .Skip((request.Page - 1) * pageSize)
            .Take(pageSize)
            .Include(release => release.Entries)
            .ToListAsync(cancellationToken);

        var items = releases
            .Select(release => Filter(release.ToDto(), request))
            .ToList();

        return new PagedResult<ReleaseDto>(items, request.Page, pageSize, totalCount, clock.UtcNow);
    }

    /// <summary>Applies the entry-level filters to the rendered groups so the page shows what was asked for.</summary>
    private static ReleaseDto Filter(ReleaseDto release, GetReleasesQuery request)
    {
        if (request.Type is null && string.IsNullOrWhiteSpace(request.Component))
        {
            return release;
        }

        var groups = release.Groups
            .Where(group => string.IsNullOrWhiteSpace(request.Component)
                            || string.Equals(group.Component, request.Component, StringComparison.OrdinalIgnoreCase))
            .Select(group => group with
            {
                Entries = group.Entries
                    .Where(entry => request.Type is null || entry.Type == request.Type)
                    .ToList()
            })
            .Where(group => group.Entries.Count > 0)
            .ToList();

        return release with { Groups = groups };
    }
}
