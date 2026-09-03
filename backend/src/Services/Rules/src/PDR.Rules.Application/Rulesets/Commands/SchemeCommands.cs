using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.Rules.Application.Abstractions;
using PDR.Rules.Domain.Rulesets;
using PDR.Rules.Domain.Schemes;

namespace PDR.Rules.Application.Rulesets.Commands;

public sealed record CreateSchemeCommand(
    string Code,
    string Name,
    string? Description,
    DateOnly? StructuredAddressMandatoryFrom) : ICommand<Guid>;

public sealed record UpdateSchemeCommand(
    string Code,
    string Name,
    string? Description,
    DateOnly? StructuredAddressMandatoryFrom,
    bool IsActive) : ICommand;

public sealed class CreateSchemeCommandValidator : AbstractValidator<CreateSchemeCommand>
{
    public CreateSchemeCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class UpdateSchemeCommandValidator : AbstractValidator<UpdateSchemeCommand>
{
    public UpdateSchemeCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class CreateSchemeCommandHandler(IRulesDbContext context)
    : IRequestHandler<CreateSchemeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(CreateSchemeCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        if (await context.Schemes.AnyAsync(scheme => scheme.Code == code, cancellationToken))
        {
            return Result.Failure<Guid>(RulesetErrors.SchemeAlreadyExists);
        }

        var scheme = Scheme.Create(code, request.Name, request.Description, request.StructuredAddressMandatoryFrom);
        context.Schemes.Add(scheme);
        await context.SaveChangesAsync(cancellationToken);

        return scheme.Id;
    }
}

public sealed class UpdateSchemeCommandHandler(IRulesDbContext context)
    : IRequestHandler<UpdateSchemeCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateSchemeCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var scheme = await context.Schemes.FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);
        if (scheme is null)
        {
            return Result.Failure(RulesetErrors.SchemeNotFound(request.Code));
        }

        scheme.Update(request.Name, request.Description, request.StructuredAddressMandatoryFrom, request.IsActive);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
