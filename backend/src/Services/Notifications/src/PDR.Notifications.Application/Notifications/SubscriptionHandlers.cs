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

public sealed record CreateSubscriptionCommand(
    string Code,
    string Name,
    string EventPattern,
    DeliveryChannel Channel,
    string Target,
    string? SchemeCodes,
    string? SourceCodes,
    NotificationSeverity MinimumSeverity,
    string? SigningSecret) : ICommand<SubscriptionDto>;

public sealed record UpdateSubscriptionCommand(
    string Code,
    string Name,
    string EventPattern,
    string? SchemeCodes,
    string? SourceCodes,
    NotificationSeverity MinimumSeverity) : ICommand<SubscriptionDto>;

public sealed record SetSubscriptionEnabledCommand(string Code, bool Enabled) : ICommand<SubscriptionDto>;

public sealed record RotateSubscriptionSecretCommand(string Code, string Secret) : ICommand<SubscriptionDto>;

public sealed record GetSubscriptionsQuery(bool IncludeDisabled = true) : IQuery<IReadOnlyList<SubscriptionDto>>;

public sealed record GetSubscriptionQuery(string Code) : IQuery<SubscriptionDto>;

public sealed class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(140);
        RuleFor(command => command.EventPattern).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Target).NotEmpty().MaximumLength(512);
    }
}

public sealed class UpdateSubscriptionCommandValidator : AbstractValidator<UpdateSubscriptionCommand>
{
    public UpdateSubscriptionCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(140);
        RuleFor(command => command.EventPattern).NotEmpty().MaximumLength(256);
    }
}

public sealed class RotateSubscriptionSecretCommandValidator : AbstractValidator<RotateSubscriptionSecretCommand>
{
    public RotateSubscriptionSecretCommandValidator() =>
        RuleFor(command => command.Secret).NotEmpty().MinimumLength(16).MaximumLength(256);
}

internal static class NotificationPageSize
{
    public static async Task<int> ResolveAsync(ISettingsReader settings, int? requested, CancellationToken cancellationToken)
    {
        var configured = await settings.GetAsync(
            NotificationSettingKeys.PageSize,
            NotificationDefaults.PageSize,
            cancellationToken);

        return Math.Clamp(requested ?? configured, 1, NotificationDefaults.MaxPageSize);
    }
}

public sealed class CreateSubscriptionCommandHandler(INotificationsDbContext context, ICurrentUser currentUser)
    : IRequestHandler<CreateSubscriptionCommand, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> HandleAsync(
        CreateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        if (await context.Subscriptions.AnyAsync(subscription => subscription.Code == code, cancellationToken))
        {
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.Duplicate(code));
        }

        var created = Subscription.Create(
            code,
            request.Name,
            request.EventPattern,
            request.Channel,
            request.Target,
            request.SchemeCodes,
            request.SourceCodes,
            request.MinimumSeverity,
            request.SigningSecret,
            currentUser.UserName);

        if (created.IsFailure)
        {
            return Result.Failure<SubscriptionDto>(created.Error);
        }

        context.Subscriptions.Add(created.Value);
        await context.SaveChangesAsync(cancellationToken);
        return created.Value.ToDto();
    }
}

public sealed class UpdateSubscriptionCommandHandler(INotificationsDbContext context)
    : IRequestHandler<UpdateSubscriptionCommand, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> HandleAsync(
        UpdateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var subscription = await context.Subscriptions
            .FirstOrDefaultAsync(item => item.Code == code, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.NotFound(request.Code));
        }

        subscription.Update(
            request.Name,
            request.EventPattern,
            request.SchemeCodes,
            request.SourceCodes,
            request.MinimumSeverity);

        await context.SaveChangesAsync(cancellationToken);
        return subscription.ToDto();
    }
}

public sealed class SetSubscriptionEnabledCommandHandler(INotificationsDbContext context)
    : IRequestHandler<SetSubscriptionEnabledCommand, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> HandleAsync(
        SetSubscriptionEnabledCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var subscription = await context.Subscriptions
            .FirstOrDefaultAsync(item => item.Code == code, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.NotFound(request.Code));
        }

        subscription.SetEnabled(request.Enabled);
        await context.SaveChangesAsync(cancellationToken);
        return subscription.ToDto();
    }
}

public sealed class RotateSubscriptionSecretCommandHandler(INotificationsDbContext context)
    : IRequestHandler<RotateSubscriptionSecretCommand, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> HandleAsync(
        RotateSubscriptionSecretCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var subscription = await context.Subscriptions
            .FirstOrDefaultAsync(item => item.Code == code, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.NotFound(request.Code));
        }

        subscription.RotateSecret(request.Secret);
        await context.SaveChangesAsync(cancellationToken);
        return subscription.ToDto();
    }
}

public sealed class GetSubscriptionsQueryHandler(INotificationsDbContext context)
    : IRequestHandler<GetSubscriptionsQuery, Result<IReadOnlyList<SubscriptionDto>>>
{
    public async Task<Result<IReadOnlyList<SubscriptionDto>>> HandleAsync(
        GetSubscriptionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Subscriptions.AsNoTracking();
        if (!request.IncludeDisabled)
        {
            query = query.Where(subscription => subscription.IsEnabled);
        }

        var subscriptions = await query
            .OrderBy(subscription => subscription.Code)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<SubscriptionDto>>([.. subscriptions.Select(item => item.ToDto())]);
    }
}

public sealed class GetSubscriptionQueryHandler(INotificationsDbContext context)
    : IRequestHandler<GetSubscriptionQuery, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> HandleAsync(
        GetSubscriptionQuery request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var subscription = await context.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Code == code, cancellationToken);

        return subscription is null
            ? Result.Failure<SubscriptionDto>(SubscriptionErrors.NotFound(request.Code))
            : subscription.ToDto();
    }
}
