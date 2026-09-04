using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.ReleaseNotes.Application.Abstractions;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.Application.Releases.Commands;

public sealed record PublishReleaseCommand(Guid Id) : ICommand;

public sealed class PublishReleaseCommandHandler(
    IReleaseNotesDbContext context,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<PublishReleaseCommand, Result>
{
    public async Task<Result> HandleAsync(PublishReleaseCommand request, CancellationToken cancellationToken)
    {
        var release = await context.Releases
            .Include(entity => entity.Entries)
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken);

        if (release is null)
        {
            return Result.Failure(ReleaseErrors.NotFound(request.Id));
        }

        var published = release.Publish(currentUser.UserName, clock.UtcNow);
        if (published.IsFailure)
        {
            return published;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
