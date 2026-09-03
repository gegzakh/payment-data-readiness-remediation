using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.Audit.Application.Abstractions;
using PDR.Audit.Domain.Ledger;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;

namespace PDR.Audit.Application.Ledger.Commands;

public sealed record AppendAuditRecordCommand(
    string Service,
    string Action,
    string EntityType,
    string EntityId,
    AuditOutcome Outcome = AuditOutcome.Success,
    string? Actor = null,
    string? ActorId = null,
    string? LegalEntity = null,
    string? Details = null,
    DateTimeOffset? OccurredAtUtc = null) : ICommand<AuditRecordDto>;

public sealed class AppendAuditRecordCommandValidator : AbstractValidator<AppendAuditRecordCommand>
{
    public AppendAuditRecordCommandValidator()
    {
        RuleFor(command => command.Service).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Action).NotEmpty().MaximumLength(128);
        RuleFor(command => command.EntityType).NotEmpty().MaximumLength(128);
        RuleFor(command => command.EntityId).NotEmpty().MaximumLength(128);
        RuleFor(command => command.LegalEntity).MaximumLength(64);
    }
}

/// <summary>
/// Appends to the hash chain. The chain lock serialises concurrent writers, because two records that
/// link to the same predecessor would fork the chain and fail verification.
/// </summary>
public sealed class AppendAuditRecordCommandHandler(
    IAuditDbContext context,
    IAuditChainLock chainLock,
    ICurrentUser currentUser,
    ICorrelationContext correlationContext,
    IClock clock) : IRequestHandler<AppendAuditRecordCommand, Result<AuditRecordDto>>
{
    public async Task<Result<AuditRecordDto>> HandleAsync(
        AppendAuditRecordCommand request,
        CancellationToken cancellationToken)
    {
        await chainLock.AcquireAsync(cancellationToken);

        var previous = await context.AuditRecords
            .OrderByDescending(record => record.Sequence)
            .FirstOrDefaultAsync(cancellationToken);

        var record = AuditRecord.Append(
            previous,
            request.OccurredAtUtc ?? clock.UtcNow,
            request.Service,
            request.Action,
            request.EntityType,
            request.EntityId,
            request.Actor ?? currentUser.UserName,
            request.ActorId ?? currentUser.UserId,
            request.Outcome,
            correlationContext.CorrelationId,
            request.LegalEntity,
            request.Details);

        context.AuditRecords.Add(record);
        await context.SaveChangesAsync(cancellationToken);

        return record.ToDto();
    }
}
