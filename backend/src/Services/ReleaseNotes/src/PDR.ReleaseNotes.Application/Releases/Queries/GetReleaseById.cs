using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Security;
using PDR.ReleaseNotes.Application.Abstractions;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.Application.Releases.Queries;

public sealed record GetReleaseByIdQuery(Guid Id) : IQuery<ReleaseDto>;

public sealed class GetReleaseByIdQueryHandler(
    IReleaseNotesDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetReleaseByIdQuery, Result<ReleaseDto>>
{
    public async Task<Result<ReleaseDto>> HandleAsync(
        GetReleaseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var release = await context.Releases
            .AsNoTracking()
            .Include(entity => entity.Entries)
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken);

        // Drafts must not be discoverable by id from the public site (IDOR).
        if (release is null ||
            (release.Status != ReleaseStatus.Published && !currentUser.HasPermission(Permissions.ReleaseNotes.Write)))
        {
            return Result.Failure<ReleaseDto>(ReleaseErrors.NotFound(request.Id));
        }

        return release.ToDto();
    }
}
