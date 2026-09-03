using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.ReleaseNotes.Application.Abstractions;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.Application.Releases.Commands;

public sealed record CreateReleaseCommand(
    string Version,
    string Title,
    DateOnly ReleaseDate,
    string? Summary,
    IReadOnlyList<ReleaseEntryInput>? Entries) : ICommand<Guid>;

public sealed class CreateReleaseCommandValidator : AbstractValidator<CreateReleaseCommand>
{
    public CreateReleaseCommandValidator()
    {
        RuleFor(command => command.Version).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Title).NotEmpty().MaximumLength(256);
        RuleForEach(command => command.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.Component).NotEmpty().MaximumLength(128);
            entry.RuleFor(e => e.Title).NotEmpty().MaximumLength(256);
        });
    }
}

public sealed class CreateReleaseCommandHandler(IReleaseNotesDbContext context)
    : IRequestHandler<CreateReleaseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(CreateReleaseCommand request, CancellationToken cancellationToken)
    {
        var versionTaken = await context.Releases
            .AsNoTracking()
            .AnyAsync(release => release.Version == request.Version, cancellationToken);

        if (versionTaken)
        {
            return Result.Failure<Guid>(ReleaseErrors.VersionAlreadyExists);
        }

        var release = Release.CreateDraft(request.Version, request.Title, request.ReleaseDate, request.Summary);

        foreach (var entry in request.Entries ?? [])
        {
            var added = release.AddEntry(
                entry.Type,
                entry.Component,
                entry.Title,
                entry.Body,
                entry.SortOrder,
                entry.References);

            if (added.IsFailure)
            {
                return Result.Failure<Guid>(added.Error);
            }
        }

        context.Releases.Add(release);
        await context.SaveChangesAsync(cancellationToken);

        return release.Id;
    }
}
