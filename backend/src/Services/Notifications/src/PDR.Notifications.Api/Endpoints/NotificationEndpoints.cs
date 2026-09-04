using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Security;
using PDR.BuildingBlocks.WebApi;
using PDR.Notifications.Application.Notifications;
using PDR.Notifications.Application.Schedules;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications").WithTags("Notifications");

        MapSubscriptions(group);
        MapNotifications(group);
        MapSchedules(group);

        return app;
    }

    private static void MapSubscriptions(RouteGroupBuilder group)
    {
        group.MapGet("/subscriptions", async (
                bool? includeDisabled,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new GetSubscriptionsQuery(includeDisabled ?? true),
                    cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Read)
            .WithName("GetSubscriptions")
            .WithSummary("Subscriptions and their delivery health. Signing secrets are never returned.")
            .Produces<IReadOnlyList<SubscriptionDto>>();

        group.MapGet("/subscriptions/{code}", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetSubscriptionQuery(code), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Read)
            .WithName("GetSubscription")
            .Produces<SubscriptionDto>();

        group.MapPost("/subscriptions", async (
                CreateSubscriptionCommand command,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(command, cancellationToken);
                return result.ToCreatedResult(
                    httpContext,
                    subscription => $"/api/v1/notifications/subscriptions/{subscription.Code}");
            })
            .RequireAuthorization(Permissions.Notifications.Write)
            .WithName("CreateSubscription")
            .WithSummary("Registers a delivery target for an event pattern and scope.")
            .Produces<SubscriptionDto>(StatusCodes.Status201Created);

        group.MapPut("/subscriptions/{code}", async (
                string code,
                UpdateSubscriptionRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateSubscriptionCommand(
                    code,
                    request.Name,
                    request.EventPattern,
                    request.SchemeCodes,
                    request.SourceCodes,
                    request.MinimumSeverity);

                var result = await sender.SendAsync(command, cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Write)
            .WithName("UpdateSubscription")
            .Produces<SubscriptionDto>();

        group.MapPost("/subscriptions/{code}/enabled", async (
                string code,
                SetEnabledRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new SetSubscriptionEnabledCommand(code, request.Enabled),
                    cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Write)
            .WithName("SetSubscriptionEnabled")
            .Produces<SubscriptionDto>();

        group.MapPost("/subscriptions/{code}/secret", async (
                string code,
                RotateSecretRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new RotateSubscriptionSecretCommand(code, request.Secret),
                    cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Admin)
            .WithName("RotateSubscriptionSecret")
            .WithSummary("Replaces the HMAC secret used to sign this subscription's outbound payloads.")
            .Produces<SubscriptionDto>();
    }

    private static void MapNotifications(RouteGroupBuilder group)
    {
        group.MapPost("/events", async (
                PublishNotificationRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                // The header is the canonical idempotency key; the body carries it for non-HTTP publishers.
                var key = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault()
                          ?? request.IdempotencyKey
                          ?? Guid.CreateVersion7().ToString();

                var command = new PublishNotificationCommand(
                    key,
                    request.EventType,
                    request.Severity,
                    request.Subject,
                    request.Payload,
                    request.SchemeCode,
                    request.SourceCode);

                var result = await sender.SendAsync(command, cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Write)
            .WithName("PublishNotification")
            .WithSummary("Publishes an event and fans it out to matching subscriptions; idempotent per key.")
            .Produces<NotificationDto>();

        group.MapGet("/events", async (
                string? eventType,
                NotificationSeverity? severity,
                int? page,
                int? pageSize,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new GetNotificationsQuery(eventType, severity, page ?? 1, pageSize),
                    cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Read)
            .WithName("GetNotifications")
            .Produces<PagedResult<NotificationDto>>();

        group.MapGet("/events/{id:guid}", async (
                Guid id,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetNotificationQuery(id), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Read)
            .WithName("GetNotification")
            .Produces<NotificationDto>();

        group.MapGet("/deliveries", async (
                DeliveryStatus? status,
                string? subscriptionCode,
                int? page,
                int? pageSize,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new GetDeliveriesQuery(status, subscriptionCode, page ?? 1, pageSize),
                    cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Read)
            .WithName("GetDeliveries")
            .WithSummary("Delivery attempts with their status, retry schedule and last error.")
            .Produces<PagedResult<DeliveryDto>>();

        group.MapPost("/deliveries/{id:guid}/replay", async (
                Guid id,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new ReplayDeliveryCommand(id), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Admin)
            .WithName("ReplayDelivery")
            .WithSummary("Requeues a dead-lettered or backing-off delivery once the endpoint is healthy again.")
            .Produces<DeliveryDto>();

        group.MapPost("/deliveries/dispatch", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new DispatchDueDeliveriesCommand(), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Admin)
            .WithName("DispatchDueDeliveries")
            .WithSummary("Drains the due deliveries now instead of waiting for the worker.")
            .Produces<DispatchSummaryDto>();
    }

    private static void MapSchedules(RouteGroupBuilder group)
    {
        group.MapGet("/scheduled-reports", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new GetScheduledReportsQuery(), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Read)
            .WithName("GetScheduledReports")
            .Produces<IReadOnlyList<ScheduledReportDto>>();

        group.MapPost("/scheduled-reports", async (
                CreateScheduledReportCommand command,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(command, cancellationToken);
                return result.ToCreatedResult(
                    httpContext,
                    report => $"/api/v1/notifications/scheduled-reports/{report.Code}");
            })
            .RequireAuthorization(Permissions.Notifications.Write)
            .WithName("CreateScheduledReport")
            .WithSummary("Schedules a dashboard for recurring delivery.")
            .Produces<ScheduledReportDto>(StatusCodes.Status201Created);

        group.MapPost("/scheduled-reports/{code}/enabled", async (
                string code,
                SetEnabledRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(
                    new SetScheduledReportEnabledCommand(code, request.Enabled),
                    cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Write)
            .WithName("SetScheduledReportEnabled")
            .Produces<ScheduledReportDto>();

        group.MapPost("/scheduled-reports/{code}/run", async (
                string code,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new RunScheduledReportCommand(code), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Write)
            .WithName("RunScheduledReport")
            .WithSummary("Runs a scheduled report immediately and moves its next slot forward.")
            .Produces<ScheduledReportDto>();

        group.MapPost("/scheduled-reports/run-due", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.SendAsync(new RunDueScheduledReportsCommand(), cancellationToken);
                return result.ToHttpResult(httpContext);
            })
            .RequireAuthorization(Permissions.Notifications.Admin)
            .WithName("RunDueScheduledReports")
            .Produces<IReadOnlyList<ScheduledReportDto>>();
    }
}

public sealed record UpdateSubscriptionRequest(
    string Name,
    string EventPattern,
    string? SchemeCodes,
    string? SourceCodes,
    NotificationSeverity MinimumSeverity);

public sealed record SetEnabledRequest(bool Enabled);

public sealed record RotateSecretRequest(string Secret);

public sealed record PublishNotificationRequest(
    string EventType,
    NotificationSeverity Severity,
    string Subject,
    string Payload,
    string? IdempotencyKey = null,
    string? SchemeCode = null,
    string? SourceCode = null);
