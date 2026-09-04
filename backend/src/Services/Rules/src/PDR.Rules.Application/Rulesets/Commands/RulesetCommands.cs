using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.Rules.Application.Abstractions;
using PDR.Rules.Domain.Rulesets;

namespace PDR.Rules.Application.Rulesets.Commands;

public sealed record CreateRulesetCommand(string SchemeCode, string Name, string? Description) : ICommand<Guid>;

public sealed record AddRulesetVersionCommand(Guid RulesetId, int? CopyFromVersionNumber, string? Notes)
    : ICommand<int>;

public sealed record AddRuleCommand(Guid RulesetId, int VersionNumber, RuleInput Rule) : ICommand<Guid>;

public sealed record RemoveRuleCommand(Guid RulesetId, int VersionNumber, Guid RuleId) : ICommand;

public sealed record ActivateRulesetVersionCommand(Guid RulesetId, int VersionNumber, DateOnly EffectiveFrom)
    : ICommand;

public sealed class CreateRulesetCommandValidator : AbstractValidator<CreateRulesetCommand>
{
    public CreateRulesetCommandValidator()
    {
        RuleFor(command => command.SchemeCode).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class AddRuleCommandValidator : AbstractValidator<AddRuleCommand>
{
    public AddRuleCommandValidator()
    {
        RuleFor(command => command.Rule.Code).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Rule.Field).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Rule.Message).NotEmpty().MaximumLength(512);
    }
}

public sealed class CreateRulesetCommandHandler(IRulesDbContext context)
    : IRequestHandler<CreateRulesetCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(CreateRulesetCommand request, CancellationToken cancellationToken)
    {
        var schemeCode = request.SchemeCode.ToUpperInvariant();

        if (!await context.Schemes.AnyAsync(scheme => scheme.Code == schemeCode, cancellationToken))
        {
            return Result.Failure<Guid>(RulesetErrors.SchemeNotFound(request.SchemeCode));
        }

        if (await context.Rulesets.AnyAsync(ruleset => ruleset.SchemeCode == schemeCode, cancellationToken))
        {
            return Result.Failure<Guid>(RulesetErrors.RulesetAlreadyExists);
        }

        var ruleset = Ruleset.Create(schemeCode, request.Name, request.Description);
        context.Rulesets.Add(ruleset);
        await context.SaveChangesAsync(cancellationToken);

        return ruleset.Id;
    }
}

public sealed class AddRulesetVersionCommandHandler(IRulesDbContext context)
    : IRequestHandler<AddRulesetVersionCommand, Result<int>>
{
    public async Task<Result<int>> HandleAsync(AddRulesetVersionCommand request, CancellationToken cancellationToken)
    {
        var ruleset = await context.Rulesets.LoadFullAsync(request.RulesetId, cancellationToken);
        if (ruleset is null)
        {
            return Result.Failure<int>(RulesetErrors.NotFound(request.RulesetId));
        }

        var version = ruleset.AddVersion(request.CopyFromVersionNumber, request.Notes);
        if (version.IsFailure)
        {
            return Result.Failure<int>(version.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return version.Value.VersionNumber;
    }
}

public sealed class AddRuleCommandHandler(IRulesDbContext context)
    : IRequestHandler<AddRuleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(AddRuleCommand request, CancellationToken cancellationToken)
    {
        var ruleset = await context.Rulesets.LoadFullAsync(request.RulesetId, cancellationToken);
        if (ruleset is null)
        {
            return Result.Failure<Guid>(RulesetErrors.NotFound(request.RulesetId));
        }

        var added = ruleset.AddRule(
            request.VersionNumber,
            request.Rule.Code,
            request.Rule.Field,
            request.Rule.Kind,
            request.Rule.Severity,
            request.Rule.Applicability,
            request.Rule.Message,
            request.Rule.Parameter);

        if (added.IsFailure)
        {
            return Result.Failure<Guid>(added.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return added.Value.Id;
    }
}

public sealed class RemoveRuleCommandHandler(IRulesDbContext context)
    : IRequestHandler<RemoveRuleCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveRuleCommand request, CancellationToken cancellationToken)
    {
        var ruleset = await context.Rulesets.LoadFullAsync(request.RulesetId, cancellationToken);
        if (ruleset is null)
        {
            return Result.Failure(RulesetErrors.NotFound(request.RulesetId));
        }

        var removed = ruleset.RemoveRule(request.VersionNumber, request.RuleId);
        if (removed.IsFailure)
        {
            return removed;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class ActivateRulesetVersionCommandHandler(
    IRulesDbContext context,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<ActivateRulesetVersionCommand, Result>
{
    public async Task<Result> HandleAsync(
        ActivateRulesetVersionCommand request,
        CancellationToken cancellationToken)
    {
        var ruleset = await context.Rulesets.LoadFullAsync(request.RulesetId, cancellationToken);
        if (ruleset is null)
        {
            return Result.Failure(RulesetErrors.NotFound(request.RulesetId));
        }

        var activated = ruleset.Activate(
            request.VersionNumber,
            request.EffectiveFrom,
            currentUser.UserName,
            clock.UtcNow);

        if (activated.IsFailure)
        {
            return activated;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal static class RulesetQueryExtensions
{
    /// <summary>Loads a ruleset with every version and rule, which the aggregate's invariants need.</summary>
    public static Task<Ruleset?> LoadFullAsync(
        this DbSet<Ruleset> rulesets,
        Guid id,
        CancellationToken cancellationToken) =>
        rulesets
            .Include(ruleset => ruleset.Versions)
            .ThenInclude(version => version.Rules)
            .FirstOrDefaultAsync(ruleset => ruleset.Id == id, cancellationToken);
}
