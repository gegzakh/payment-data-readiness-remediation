using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.ReleaseNotes.Application.Abstractions;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.Application.Releases.Commands;

public sealed record AddReleaseEntryCommand(Guid ReleaseId, ReleaseEntryInput Entry) : ICommand<Guid>;

public sealed record UpdateReleaseEntryCommand(Guid ReleaseId, Guid EntryId, ReleaseEntryInput Entry) : ICommand;

public sealed record RemoveReleaseEntryCommand(Guid ReleaseId, Guid EntryId) : ICommand;

public sealed record AddErratumCommand(
    Guid ReleaseId,
    string Component,
    string Title,
    string? Body,
    IReadOnlyList<string>? References) : ICommand<Guid>;

public sealed class AddReleaseEntryCommandValidator : AbstractValidator<AddReleaseEntryCommand>
{
    public AddReleaseEntryCommandValidator()
    {
        RuleFor(command => command.Entry.Component).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Entry.Title).NotEmpty().MaximumLength(256);
    }
}

public sealed class UpdateReleaseEntryCommandValidator : AbstractValidator<UpdateReleaseEntryCommand>
{
    public UpdateReleaseEntryCommandValidator()
    {
        RuleFor(command => command.Entry.Component).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Entry.Title).NotEmpty().MaximumLength(256);
    }
}

public sealed class AddErratumCommandValidator : AbstractValidator<AddErratumCommand>
{
    public AddErratumCommandValidator()
    {
        RuleFor(command => command.Component).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Title).NotEmpty().MaximumLength(256);
    }
}

public sealed class AddReleaseEntryCommandHandler(IReleaseNotesDbContext context)
    : IRequestHandler<AddReleaseEntryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(AddReleaseEntryCommand request, CancellationToken cancellationToken)
    {
        var release = await context.Releases
            .Include(entity => entity.Entries)
            .FirstOrDefaultAsync(entity => entity.Id == request.ReleaseId, cancellationToken);

        if (release is null)
        {
            return Result.Failure<Guid>(ReleaseErrors.NotFound(request.ReleaseId));
        }

        var added = release.AddEntry(
            request.Entry.Type,
            request.Entry.Component,
            request.Entry.Title,
            request.Entry.Body,
            request.Entry.SortOrder,
            request.Entry.References);

        if (added.IsFailure)
        {
            return Result.Failure<Guid>(added.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return added.Value.Id;
    }
}

public sealed class UpdateReleaseEntryCommandHandler(IReleaseNotesDbContext context)
    : IRequestHandler<UpdateReleaseEntryCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateReleaseEntryCommand request, CancellationToken cancellationToken)
    {
        var release = await context.Releases
            .Include(entity => entity.Entries)
            .FirstOrDefaultAsync(entity => entity.Id == request.ReleaseId, cancellationToken);

        if (release is null)
        {
            return Result.Failure(ReleaseErrors.NotFound(request.ReleaseId));
        }

        var updated = release.UpdateEntry(
            request.EntryId,
            request.Entry.Type,
            request.Entry.Component,
            request.Entry.Title,
            request.Entry.Body,
            request.Entry.SortOrder ?? 0,
            request.Entry.References);

        if (updated.IsFailure)
        {
            return updated;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class RemoveReleaseEntryCommandHandler(IReleaseNotesDbContext context)
    : IRequestHandler<RemoveReleaseEntryCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveReleaseEntryCommand request, CancellationToken cancellationToken)
    {
        var release = await context.Releases
            .Include(entity => entity.Entries)
            .FirstOrDefaultAsync(entity => entity.Id == request.ReleaseId, cancellationToken);

        if (release is null)
        {
            return Result.Failure(ReleaseErrors.NotFound(request.ReleaseId));
        }

        var removed = release.RemoveEntry(request.EntryId);
        if (removed.IsFailure)
        {
            return removed;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class AddErratumCommandHandler(IReleaseNotesDbContext context)
    : IRequestHandler<AddErratumCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(AddErratumCommand request, CancellationToken cancellationToken)
    {
        var release = await context.Releases
            .Include(entity => entity.Entries)
            .FirstOrDefaultAsync(entity => entity.Id == request.ReleaseId, cancellationToken);

        if (release is null)
        {
            return Result.Failure<Guid>(ReleaseErrors.NotFound(request.ReleaseId));
        }

        var added = release.AddErratum(request.Component, request.Title, request.Body, request.References);
        if (added.IsFailure)
        {
            return Result.Failure<Guid>(added.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return added.Value.Id;
    }
}
