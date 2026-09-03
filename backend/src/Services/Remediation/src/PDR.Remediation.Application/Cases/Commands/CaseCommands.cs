using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.Remediation.Application.Abstractions;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Application.Cases.Commands;

/// <summary>Creates or refreshes remediation cases from a validation run (FR-REM-001).</summary>
public sealed record GenerateCasesCommand(Guid? RunId) : ICommand<CaseGenerationDto>;

/// <summary>A maker writes or revises the proposed structured address (FR-WF-002).</summary>
public sealed record ProposeCorrectionCommand(
    Guid CaseId,
    string? Country,
    string? TownName,
    string? PostCode,
    string? StreetName,
    string? BuildingNumber,
    string? Notes) : ICommand<CaseDetailDto>;

public sealed record AddCaseEvidenceCommand(
    Guid CaseId,
    string Kind,
    string Reference,
    string? Description) : ICommand<CaseDetailDto>;

public sealed record AssignCaseCommand(
    Guid CaseId,
    string Queue,
    string? AssignedTo,
    DateOnly? DueDate) : ICommand<CaseDetailDto>;

public sealed record SubmitCaseCommand(Guid CaseId) : ICommand<CaseDetailDto>;

/// <summary>The checker's verdict; four-eyes and rationale rules live in the aggregate (FR-WF-003).</summary>
public sealed record DecideCaseCommand(
    Guid CaseId,
    DecisionType Decision,
    string? Rationale,
    DateOnly? ExceptionExpiresOn) : ICommand<CaseDetailDto>;

public sealed class ProposeCorrectionCommandValidator : AbstractValidator<ProposeCorrectionCommand>
{
    public ProposeCorrectionCommandValidator()
    {
        RuleFor(command => command.CaseId).NotEmpty();
        RuleFor(command => command.Country).MaximumLength(8);
        RuleFor(command => command.TownName).MaximumLength(140);
        RuleFor(command => command.PostCode).MaximumLength(32);
        RuleFor(command => command.StreetName).MaximumLength(140);
        RuleFor(command => command.BuildingNumber).MaximumLength(32);
    }
}

public sealed class AddCaseEvidenceCommandValidator : AbstractValidator<AddCaseEvidenceCommand>
{
    public AddCaseEvidenceCommandValidator()
    {
        RuleFor(command => command.CaseId).NotEmpty();
        RuleFor(command => command.Kind).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Reference).NotEmpty().MaximumLength(512);
        RuleFor(command => command.Description).MaximumLength(1024);
    }
}

public sealed class AssignCaseCommandValidator : AbstractValidator<AssignCaseCommand>
{
    public AssignCaseCommandValidator()
    {
        RuleFor(command => command.CaseId).NotEmpty();
        RuleFor(command => command.Queue).NotEmpty().MaximumLength(64);
        RuleFor(command => command.AssignedTo).MaximumLength(128);
    }
}

public sealed class DecideCaseCommandValidator : AbstractValidator<DecideCaseCommand>
{
    public DecideCaseCommandValidator()
    {
        RuleFor(command => command.CaseId).NotEmpty();
        RuleFor(command => command.Rationale).MaximumLength(1024);
    }
}

public sealed class GenerateCasesCommandHandler(CaseGenerator generator)
    : IRequestHandler<GenerateCasesCommand, Result<CaseGenerationDto>>
{
    public Task<Result<CaseGenerationDto>> HandleAsync(
        GenerateCasesCommand request,
        CancellationToken cancellationToken) =>
        generator.GenerateAsync(request.RunId, cancellationToken);
}

/// <summary>Shared plumbing: load a case with its children, act on it, save, return the detail.</summary>
internal sealed class CaseWorkflow(IRemediationDbContext context, IClock clock)
{
    public DateOnly Today => DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

    public Task<RemediationCase?> LoadAsync(Guid caseId, CancellationToken cancellationToken) =>
        context.Cases
            .Include(entity => entity.Proposal)
            .Include(entity => entity.Evidence)
            .Include(entity => entity.History)
            .FirstOrDefaultAsync(entity => entity.Id == caseId, cancellationToken);

    public async Task<Result<CaseDetailDto>> CommitAsync(RemediationCase entity, CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToDetail(Today);
    }
}

public sealed class ProposeCorrectionCommandHandler(
    IRemediationDbContext context,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<ProposeCorrectionCommand, Result<CaseDetailDto>>
{
    private readonly CaseWorkflow _workflow = new(context, clock);

    public async Task<Result<CaseDetailDto>> HandleAsync(
        ProposeCorrectionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _workflow.LoadAsync(request.CaseId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<CaseDetailDto>(RemediationErrors.CaseNotFound(request.CaseId));
        }

        // A human edit is authoritative: the maker vouches for the values, so confidence is full.
        var address = new ProposedAddress(
            request.Country,
            request.TownName,
            request.PostCode,
            request.StreetName,
            request.BuildingNumber,
            FieldConfidence.Certain);

        var result = entity.Propose(
            ProposalMethod.ManualEdit,
            address,
            request.Notes,
            currentUser.UserName,
            clock.UtcNow);

        return result.IsFailure
            ? Result.Failure<CaseDetailDto>(result.Error)
            : await _workflow.CommitAsync(entity, cancellationToken);
    }
}

public sealed class AddCaseEvidenceCommandHandler(
    IRemediationDbContext context,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<AddCaseEvidenceCommand, Result<CaseDetailDto>>
{
    private readonly CaseWorkflow _workflow = new(context, clock);

    public async Task<Result<CaseDetailDto>> HandleAsync(
        AddCaseEvidenceCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _workflow.LoadAsync(request.CaseId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<CaseDetailDto>(RemediationErrors.CaseNotFound(request.CaseId));
        }

        var result = entity.AddEvidence(
            request.Kind,
            request.Reference,
            request.Description,
            currentUser.UserName,
            clock.UtcNow);

        return result.IsFailure
            ? Result.Failure<CaseDetailDto>(result.Error)
            : await _workflow.CommitAsync(entity, cancellationToken);
    }
}

public sealed class AssignCaseCommandHandler(
    IRemediationDbContext context,
    ISettingsReader settings,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<AssignCaseCommand, Result<CaseDetailDto>>
{
    private readonly CaseWorkflow _workflow = new(context, clock);

    public async Task<Result<CaseDetailDto>> HandleAsync(
        AssignCaseCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _workflow.LoadAsync(request.CaseId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<CaseDetailDto>(RemediationErrors.CaseNotFound(request.CaseId));
        }

        var slaDays = await settings.GetAsync(
            RemediationSettingKeys.SlaDays,
            RemediationDefaults.DefaultSlaDays,
            cancellationToken);

        entity.Assign(
            request.Queue,
            request.AssignedTo,
            request.DueDate ?? _workflow.Today.AddDays(slaDays),
            currentUser.UserName,
            clock.UtcNow);

        return await _workflow.CommitAsync(entity, cancellationToken);
    }
}

public sealed class SubmitCaseCommandHandler(
    IRemediationDbContext context,
    ISettingsReader settings,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<SubmitCaseCommand, Result<CaseDetailDto>>
{
    private readonly CaseWorkflow _workflow = new(context, clock);

    public async Task<Result<CaseDetailDto>> HandleAsync(
        SubmitCaseCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _workflow.LoadAsync(request.CaseId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<CaseDetailDto>(RemediationErrors.CaseNotFound(request.CaseId));
        }

        var policy = await settings.GetAsync(
            RemediationSettingKeys.EvidenceRequiredForNewData,
            RemediationDefaults.EvidenceRequiredForNewData,
            cancellationToken);

        var result = entity.Submit(
            currentUser.UserName,
            policy && AddsNewData(entity),
            clock.UtcNow);

        return result.IsFailure
            ? Result.Failure<CaseDetailDto>(result.Error)
            : await _workflow.CommitAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Restructuring what the source already holds is self-evidencing; inventing a value the source never
    /// had is not, and needs something to back it (FR-WF-004).
    /// </summary>
    private static bool AddsNewData(RemediationCase entity)
    {
        if (entity.Proposal is not { } proposal)
        {
            return false;
        }

        var original = (entity.OriginalAddressLines ?? string.Empty)
            + entity.OriginalCountry + entity.OriginalTownName + entity.OriginalPostCode
            + entity.OriginalStreetName + entity.OriginalBuildingNumber;

        return new[]
            {
                proposal.Country, proposal.TownName, proposal.PostCode,
                proposal.StreetName, proposal.BuildingNumber
            }
            .Any(value => !string.IsNullOrWhiteSpace(value)
                          && !original.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class DecideCaseCommandHandler(
    IRemediationDbContext context,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<DecideCaseCommand, Result<CaseDetailDto>>
{
    private readonly CaseWorkflow _workflow = new(context, clock);

    public async Task<Result<CaseDetailDto>> HandleAsync(
        DecideCaseCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _workflow.LoadAsync(request.CaseId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<CaseDetailDto>(RemediationErrors.CaseNotFound(request.CaseId));
        }

        var result = entity.Decide(
            request.Decision,
            currentUser.UserName,
            request.Rationale,
            request.ExceptionExpiresOn,
            clock.UtcNow);

        return result.IsFailure
            ? Result.Failure<CaseDetailDto>(result.Error)
            : await _workflow.CommitAsync(entity, cancellationToken);
    }
}
