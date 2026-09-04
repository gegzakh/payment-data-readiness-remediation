using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.Sources.Application.Abstractions;
using PDR.Sources.Domain.Inventory;

namespace PDR.Sources.Application.Inventory.Commands;

public sealed record RegisterSourceCommand(
    string Code,
    string Name,
    SourceKind Kind,
    InterfaceKind Interface,
    string OwnerName,
    string OwnerEmail,
    string LegalEntity,
    IReadOnlyList<string> SchemeCodes,
    string? Schedule,
    long EstimatedPartyCount,
    long RecurringInstructionCount,
    bool IsAuthoritative) : ICommand<Guid>;

public sealed record UpdateSourceCommand(
    string Code,
    string Name,
    SourceKind Kind,
    InterfaceKind Interface,
    string OwnerName,
    string OwnerEmail,
    string LegalEntity,
    IReadOnlyList<string> SchemeCodes,
    string? Schedule,
    long EstimatedPartyCount,
    long RecurringInstructionCount,
    bool IsAuthoritative,
    OnboardingStatus Status,
    MappingReadiness Mapping,
    string? RemediationOwner,
    bool IsActive) : ICommand;

public sealed record AddFieldMappingCommand(string Code, FieldMappingInput Mapping) : ICommand;

public sealed record RemoveFieldMappingCommand(string Code, Guid MappingId) : ICommand;

public sealed record ReplaceLineageCommand(string Code, IReadOnlyList<LineageStepInput> Steps) : ICommand;

public sealed record RecordScanCommand(string Code, decimal CoveragePercent) : ICommand;

public sealed record AttestSourceCommand(string Code) : ICommand;

public sealed class RegisterSourceCommandValidator : AbstractValidator<RegisterSourceCommand>
{
    public RegisterSourceCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
        RuleFor(command => command.OwnerName).NotEmpty().MaximumLength(128);
        RuleFor(command => command.OwnerEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.LegalEntity).NotEmpty().MaximumLength(64);
        RuleFor(command => command.SchemeCodes).NotEmpty();
        RuleFor(command => command.EstimatedPartyCount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.RecurringInstructionCount).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateSourceCommandValidator : AbstractValidator<UpdateSourceCommand>
{
    public UpdateSourceCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
        RuleFor(command => command.OwnerName).NotEmpty().MaximumLength(128);
        RuleFor(command => command.OwnerEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.LegalEntity).NotEmpty().MaximumLength(64);
        RuleFor(command => command.SchemeCodes).NotEmpty();
    }
}

public sealed class AddFieldMappingCommandValidator : AbstractValidator<AddFieldMappingCommand>
{
    public AddFieldMappingCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty();
        RuleFor(command => command.Mapping.SourceAttribute).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Mapping.TargetElement).NotEmpty().MaximumLength(128);
    }
}

public sealed class ReplaceLineageCommandValidator : AbstractValidator<ReplaceLineageCommand>
{
    public ReplaceLineageCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty();
        RuleForEach(command => command.Steps).ChildRules(step =>
        {
            step.RuleFor(entry => entry.FromNode).NotEmpty().MaximumLength(128);
            step.RuleFor(entry => entry.ToNode).NotEmpty().MaximumLength(128);
        });
    }
}

public sealed class RecordScanCommandValidator : AbstractValidator<RecordScanCommand>
{
    public RecordScanCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty();
        RuleFor(command => command.CoveragePercent).InclusiveBetween(0, 100);
    }
}

public sealed class RegisterSourceCommandHandler(ISourcesDbContext context)
    : IRequestHandler<RegisterSourceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(RegisterSourceCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        if (await context.SourceSystems.AnyAsync(source => source.Code == code, cancellationToken))
        {
            return Result.Failure<Guid>(SourceErrors.AlreadyExists);
        }

        var source = SourceSystem.Register(
            code,
            request.Name,
            request.Kind,
            request.Interface,
            request.OwnerName,
            request.OwnerEmail,
            request.LegalEntity,
            string.Join(',', request.SchemeCodes),
            request.Schedule,
            request.EstimatedPartyCount,
            request.RecurringInstructionCount,
            request.IsAuthoritative);

        context.SourceSystems.Add(source);
        await context.SaveChangesAsync(cancellationToken);

        return source.Id;
    }
}

public sealed class UpdateSourceCommandHandler(ISourcesDbContext context)
    : IRequestHandler<UpdateSourceCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateSourceCommand request, CancellationToken cancellationToken)
    {
        var source = await context.FindSourceAsync(request.Code, cancellationToken);
        if (source is null)
        {
            return Result.Failure(SourceErrors.NotFound(request.Code));
        }

        source.Update(
            request.Name,
            request.Kind,
            request.Interface,
            request.OwnerName,
            request.OwnerEmail,
            request.LegalEntity,
            string.Join(',', request.SchemeCodes),
            request.Schedule,
            request.EstimatedPartyCount,
            request.RecurringInstructionCount,
            request.IsAuthoritative,
            request.Status,
            request.Mapping,
            request.RemediationOwner,
            request.IsActive);

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class AddFieldMappingCommandHandler(ISourcesDbContext context)
    : IRequestHandler<AddFieldMappingCommand, Result>
{
    public async Task<Result> HandleAsync(AddFieldMappingCommand request, CancellationToken cancellationToken)
    {
        var source = await context.FindSourceAsync(request.Code, cancellationToken);
        if (source is null)
        {
            return Result.Failure(SourceErrors.NotFound(request.Code));
        }

        var result = source.AddMapping(
            request.Mapping.SourceAttribute,
            request.Mapping.TargetElement,
            request.Mapping.Transformation,
            request.Mapping.IsAuthoritative,
            request.Mapping.Notes);

        if (result.IsFailure)
        {
            return result;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class RemoveFieldMappingCommandHandler(ISourcesDbContext context)
    : IRequestHandler<RemoveFieldMappingCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveFieldMappingCommand request, CancellationToken cancellationToken)
    {
        var source = await context.FindSourceAsync(request.Code, cancellationToken);
        if (source is null)
        {
            return Result.Failure(SourceErrors.NotFound(request.Code));
        }

        var result = source.RemoveMapping(request.MappingId);
        if (result.IsFailure)
        {
            return result;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class ReplaceLineageCommandHandler(ISourcesDbContext context)
    : IRequestHandler<ReplaceLineageCommand, Result>
{
    public async Task<Result> HandleAsync(ReplaceLineageCommand request, CancellationToken cancellationToken)
    {
        var source = await context.FindSourceAsync(request.Code, cancellationToken);
        if (source is null)
        {
            return Result.Failure(SourceErrors.NotFound(request.Code));
        }

        source.ReplaceLineage(request.Steps.Select(step =>
            (step.FromNode, step.ToNode, step.Channel, step.Description)));

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class RecordScanCommandHandler(ISourcesDbContext context, IClock clock)
    : IRequestHandler<RecordScanCommand, Result>
{
    public async Task<Result> HandleAsync(RecordScanCommand request, CancellationToken cancellationToken)
    {
        var source = await context.FindSourceAsync(request.Code, cancellationToken);
        if (source is null)
        {
            return Result.Failure(SourceErrors.NotFound(request.Code));
        }

        var result = source.RecordScan(request.CoveragePercent, clock.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class AttestSourceCommandHandler(ISourcesDbContext context, ICurrentUser currentUser, IClock clock)
    : IRequestHandler<AttestSourceCommand, Result>
{
    public async Task<Result> HandleAsync(AttestSourceCommand request, CancellationToken cancellationToken)
    {
        var source = await context.FindSourceAsync(request.Code, cancellationToken);
        if (source is null)
        {
            return Result.Failure(SourceErrors.NotFound(request.Code));
        }

        source.Attest(currentUser.UserName, clock.UtcNow);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

internal static class SourcesDbContextExtensions
{
    /// <summary>Loads the aggregate with the children every command may mutate.</summary>
    public static Task<SourceSystem?> FindSourceAsync(
        this ISourcesDbContext context,
        string code,
        CancellationToken cancellationToken)
    {
        var normalized = code.ToUpperInvariant();

        return context.SourceSystems
            .Include(source => source.Mappings)
            .Include(source => source.Lineage)
            .FirstOrDefaultAsync(source => source.Code == normalized, cancellationToken);
    }
}
