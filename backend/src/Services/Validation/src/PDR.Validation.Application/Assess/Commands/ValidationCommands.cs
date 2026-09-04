using FluentValidation;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.Validation.Application.Assess.Queries;

namespace PDR.Validation.Application.Assess.Commands;

/// <summary>Validates one parsed ingestion batch against the current and post-cutover rule sets.</summary>
public sealed record RunValidationCommand(Guid BatchId, DateOnly? AsOf = null) : ICommand<ValidationRunDto>;

public sealed class RunValidationCommandValidator : AbstractValidator<RunValidationCommand>
{
    public RunValidationCommandValidator() => RuleFor(command => command.BatchId).NotEmpty();
}

public sealed class RunValidationCommandHandler(ValidationEngine engine, ISettingsReader settings)
    : IRequestHandler<RunValidationCommand, Result<ValidationRunDto>>
{
    public async Task<Result<ValidationRunDto>> HandleAsync(
        RunValidationCommand request,
        CancellationToken cancellationToken)
    {
        var defaultScheme = await settings.GetAsync(
            ValidationSettingKeys.DefaultSchemeCode,
            ValidationDefaults.SchemeCode,
            cancellationToken);

        var result = await engine.RunAsync(request.BatchId, defaultScheme, request.AsOf, cancellationToken);

        return result.IsFailure
            ? Result.Failure<ValidationRunDto>(result.Error)
            : result.Value.ToDto();
    }
}
