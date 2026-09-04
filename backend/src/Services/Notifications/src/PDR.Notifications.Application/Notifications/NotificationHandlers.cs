using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.Notifications.Application.Abstractions;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.Application.Notifications;

public sealed record PublishNotificationCommand(
    string IdempotencyKey,
    string EventType,
    NotificationSeverity Severity,
    string Subject,
    string Payload,
    string? SchemeCode = null,
    string? SourceCode = null) : ICommand<NotificationDto>;

public sealed record GetNotificationsQuery(
    string? EventType = null,
    NotificationSeverity? Severity = null,
    int Page = 1,
    int? PageSize = null) : IQuery<PagedResult<NotificationDto>>;

public sealed record GetNotificationQuery(Guid Id) : IQuery<NotificationDto>;

public sealed record GetDeliveriesQuery(
    DeliveryStatus? Status = null,
    string? SubscriptionCode = null,
    int Page = 1,
    int? PageSize = null) : IQuery<PagedResult<DeliveryDto>>;

public sealed record ReplayDeliveryCommand(Guid DeliveryId) : ICommand<DeliveryDto>;

public sealed record DispatchDueDeliveriesCommand : ICommand<DispatchSummaryDto>;

public sealed class PublishNotificationCommandValidator : AbstractValidator<PublishNotificationCommand>
{
    public PublishNotificationCommandValidator()
    {
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleFor(command => command.EventType).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Subject).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Payload).NotEmpty();
    }
}

/// <summary>
/// Publishing is idempotent on the caller's key: a repeat of the same publish returns the original
/// notification and its deliveries untouched, so an upstream retry cannot double-notify anyone.
/// </summary>
public sealed class PublishNotificationCommandHandler(
    INotificationsDbContext context,
    ISettingsReader settings,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<PublishNotificationCommand, Result<NotificationDto>>
{
    public async Task<Result<NotificationDto>> HandleAsync(
        PublishNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await context.Notifications
            .Include(notification => notification.Deliveries)
            .FirstOrDefaultAsync(
                notification => notification.IdempotencyKey == request.IdempotencyKey,
                cancellationToken);

        if (existing is not null)
        {
            return existing.ToDto();
        }

        var limit = await settings.GetAsync(
            NotificationSettingKeys.MaxPayloadBytes,
            NotificationDefaults.MaxPayloadBytes,
            cancellationToken);
        var size = Encoding.UTF8.GetByteCount(request.Payload);
        if (size > limit)
        {
            return Result.Failure<NotificationDto>(NotificationErrors.PayloadTooLarge(size, limit));
        }

        var now = clock.UtcNow;
        var notification = Notification.Publish(
            request.IdempotencyKey,
            request.EventType,
            request.Severity,
            request.Subject,
            request.Payload,
            request.SchemeCode,
            request.SourceCode,
            currentUser.UserName,
            now);

        var subscriptions = await context.Subscriptions
            .Where(subscription => subscription.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var subscription in subscriptions.Where(subscription =>
                     subscription.Matches(notification.EventType, notification.Severity, notification.SchemeCode, notification.SourceCode)))
        {
            notification.AddDelivery(subscription, now);
        }

        context.Notifications.Add(notification);
        await context.SaveChangesAsync(cancellationToken);
        return notification.ToDto();
    }
}

public sealed class GetNotificationsQueryHandler(INotificationsDbContext context, ISettingsReader settings, IClock clock)
    : IRequestHandler<GetNotificationsQuery, Result<PagedResult<NotificationDto>>>
{
    public async Task<Result<PagedResult<NotificationDto>>> HandleAsync(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = await NotificationPageSize.ResolveAsync(settings, request.PageSize, cancellationToken);
        var page = Math.Max(request.Page, 1);

        var query = context.Notifications.AsNoTracking().Include(notification => notification.Deliveries).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            var eventType = request.EventType.ToLowerInvariant();
            query = query.Where(notification => notification.EventType == eventType);
        }

        if (request.Severity is not null)
        {
            query = query.Where(notification => notification.Severity == request.Severity);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(notification => notification.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>(
            [.. items.Select(item => item.ToDto())],
            page,
            pageSize,
            total,
            clock.UtcNow);
    }
}

public sealed class GetNotificationQueryHandler(INotificationsDbContext context)
    : IRequestHandler<GetNotificationQuery, Result<NotificationDto>>
{
    public async Task<Result<NotificationDto>> HandleAsync(
        GetNotificationQuery request,
        CancellationToken cancellationToken)
    {
        var notification = await context.Notifications
            .AsNoTracking()
            .Include(item => item.Deliveries)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        return notification is null
            ? Result.Failure<NotificationDto>(NotificationErrors.NotFound(request.Id))
            : notification.ToDto();
    }
}

public sealed class GetDeliveriesQueryHandler(INotificationsDbContext context, ISettingsReader settings, IClock clock)
    : IRequestHandler<GetDeliveriesQuery, Result<PagedResult<DeliveryDto>>>
{
    public async Task<Result<PagedResult<DeliveryDto>>> HandleAsync(
        GetDeliveriesQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = await NotificationPageSize.ResolveAsync(settings, request.PageSize, cancellationToken);
        var page = Math.Max(request.Page, 1);

        var query = context.Deliveries.AsNoTracking().AsQueryable();
        if (request.Status is not null)
        {
            query = query.Where(delivery => delivery.Status == request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.SubscriptionCode))
        {
            var code = request.SubscriptionCode.ToUpperInvariant();
            query = query.Where(delivery => delivery.SubscriptionCode == code);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(delivery => delivery.QueuedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DeliveryDto>(
            [.. items.Select(item => item.ToDto())],
            page,
            pageSize,
            total,
            clock.UtcNow);
    }
}

public sealed class ReplayDeliveryCommandHandler(INotificationsDbContext context, IClock clock)
    : IRequestHandler<ReplayDeliveryCommand, Result<DeliveryDto>>
{
    public async Task<Result<DeliveryDto>> HandleAsync(
        ReplayDeliveryCommand request,
        CancellationToken cancellationToken)
    {
        var delivery = await context.Deliveries
            .FirstOrDefaultAsync(item => item.Id == request.DeliveryId, cancellationToken);

        if (delivery is null)
        {
            return Result.Failure<DeliveryDto>(NotificationErrors.DeliveryNotFound(request.DeliveryId));
        }

        if (delivery.Status is not (DeliveryStatus.DeadLettered or DeliveryStatus.Retrying))
        {
            return Result.Failure<DeliveryDto>(NotificationErrors.DeliveryNotReplayable);
        }

        delivery.Replay(clock.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
        return delivery.ToDto();
    }
}

public sealed class DispatchDueDeliveriesCommandHandler(DeliveryDispatcher dispatcher)
    : IRequestHandler<DispatchDueDeliveriesCommand, Result<DispatchSummaryDto>>
{
    public async Task<Result<DispatchSummaryDto>> HandleAsync(
        DispatchDueDeliveriesCommand request,
        CancellationToken cancellationToken) =>
        await dispatcher.DispatchDueAsync(cancellationToken);
}
