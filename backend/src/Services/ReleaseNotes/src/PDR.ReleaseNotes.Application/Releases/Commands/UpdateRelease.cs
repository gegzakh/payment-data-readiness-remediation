using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.ReleaseNotes.Application.Abstractions;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.Application.Releases.Commands;

public sealed record UpdateReleaseCommand(
    Guid Id,
    string Version,
    string Title,
    DateOnly ReleaseDate,
    string? Summary) : ICommand;

public sealed class UpdateReleaseCommandValidator : AbstractValidator<UpdateReleaseCommand>
{
    public UpdateReleaseCommandValidator()
    {
        RuleFor(command => command.Version).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Title).NotEmpty().MaximumLength(256);
    }
}

public sealed class UpdateReleaseCommandHandler(IReleaseNotesDbContext context)
    : IRequestHandler<UpdateReleaseCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateReleaseCommand request, CancellationToken cancellationToken)
    {
        var release = await context.Releases
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken);

        if (release is null)
        {
            return Result.Failure(ReleaseErrors.NotFound(request.Id));
        }

        var versionTaken = await context.Releases
            .AsNoTracking()
            .AnyAsync(
                entity => entity.Version == request.Version && entity.Id != request.Id,
                cancellationToken);

        if (versionTaken)
        {
            return Result.Failure(ReleaseErrors.VersionAlreadyExists);
        }

        var updated = release.UpdateDetails(request.Version, request.Title, request.ReleaseDate, request.Summary);
        if (updated.IsFailure)
        {
            return updated;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
