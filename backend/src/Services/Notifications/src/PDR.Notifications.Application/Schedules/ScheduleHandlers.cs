using System.Globalization;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.Notifications.Application.Abstractions;
using PDR.Notifications.Application.Notifications;
using PDR.Notifications.Domain.Schedules;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.Application.Schedules;

public sealed record CreateScheduledReportCommand(
    string Code,
    string Name,
    string Audience,
    string? SchemeCodes,
    string? SourceCodes,
    ScheduleFrequency Frequency,
    int HourUtc,
    int DayOfWeek,
    int DayOfMonth,
    string Recipients) : ICommand<ScheduledReportDto>;

public sealed record SetScheduledReportEnabledCommand(string Code, bool Enabled) : ICommand<ScheduledReportDto>;

public sealed record GetScheduledReportsQuery : IQuery<IReadOnlyList<ScheduledReportDto>>;

/// <summary>Runs every report whose slot has passed; also exposed so operators can force a run.</summary>
public sealed record RunDueScheduledReportsCommand : ICommand<IReadOnlyList<ScheduledReportDto>>;

public sealed record RunScheduledReportCommand(string Code) : ICommand<ScheduledReportDto>;

public sealed class CreateScheduledReportCommandValidator : AbstractValidator<CreateScheduledReportCommand>
{
    public CreateScheduledReportCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(140);
        RuleFor(command => command.Audience).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Recipients).NotEmpty().MaximumLength(1024);
    }
}

public sealed class CreateScheduledReportCommandHandler(
    INotificationsDbContext context,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<CreateScheduledReportCommand, Result<ScheduledReportDto>>
{
    public async Task<Result<ScheduledReportDto>> HandleAsync(
        CreateScheduledReportCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        if (await context.ScheduledReports.AnyAsync(report => report.Code == code, cancellationToken))
        {
            return Result.Failure<ScheduledReportDto>(ScheduleErrors.Duplicate(code));
        }

        var created = ScheduledReport.Create(
            code,
            request.Name,
            request.Audience,
            request.SchemeCodes,
            request.SourceCodes,
            request.Frequency,
            request.HourUtc,
            request.DayOfWeek,
            request.DayOfMonth,
            request.Recipients,
            currentUser.UserName,
            clock.UtcNow);

        if (created.IsFailure)
        {
            return Result.Failure<ScheduledReportDto>(created.Error);
        }

        context.ScheduledReports.Add(created.Value);
        await context.SaveChangesAsync(cancellationToken);
        return created.Value.ToDto();
    }
}

public sealed class SetScheduledReportEnabledCommandHandler(INotificationsDbContext context, IClock clock)
    : IRequestHandler<SetScheduledReportEnabledCommand, Result<ScheduledReportDto>>
{
    public async Task<Result<ScheduledReportDto>> HandleAsync(
        SetScheduledReportEnabledCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var report = await context.ScheduledReports
            .FirstOrDefaultAsync(item => item.Code == code, cancellationToken);

        if (report is null)
        {
            return Result.Failure<ScheduledReportDto>(ScheduleErrors.NotFound(request.Code));
        }

        report.SetEnabled(request.Enabled, clock.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
        return report.ToDto();
    }
}

public sealed class GetScheduledReportsQueryHandler(INotificationsDbContext context)
    : IRequestHandler<GetScheduledReportsQuery, Result<IReadOnlyList<ScheduledReportDto>>>
{
    public async Task<Result<IReadOnlyList<ScheduledReportDto>>> HandleAsync(
        GetScheduledReportsQuery request,
        CancellationToken cancellationToken)
    {
        var reports = await context.ScheduledReports
            .AsNoTracking()
            .OrderBy(report => report.Code)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ScheduledReportDto>>([.. reports.Select(item => item.ToDto())]);
    }
}

/// <summary>
/// A scheduled report is delivered as a notification like everything else, so it inherits subscription
/// routing, signing, retries and the delivery audit rather than owning a second delivery path.
/// </summary>
public sealed class ScheduledReportRunner(INotificationsDbContext context, IClock clock)
{
    public async Task<IReadOnlyList<ScheduledReport>> RunAsync(
        IReadOnlyList<ScheduledReport> reports,
        CancellationToken cancellationToken)
    {
        if (reports.Count == 0)
        {
            return reports;
        }

        var now = clock.UtcNow;
        var subscriptions = await context.Subscriptions
            .Where(subscription => subscription.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var report in reports)
        {
            var eventType = $"report.{report.Audience}";
            var key = $"report:{report.Code}:{now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}";
            var payload = JsonSerializer.Serialize(new
            {
                report = report.Code,
                report.Name,
                report.Audience,
                schemeCodes = report.SchemeCodes,
                sourceCodes = report.SourceCodes,
                recipients = report.Recipients,
                generatedAtUtc = now
            });

            var notification = Notification.Publish(
                key,
                eventType,
                NotificationSeverity.Info,
                $"{report.Name} ({report.Audience} dashboard)",
                payload,
                report.SchemeCodes?.Split(',').FirstOrDefault(),
                report.SourceCodes?.Split(',').FirstOrDefault(),
                report.Owner,
                now);

            foreach (var subscription in subscriptions.Where(subscription =>
                         subscription.Matches(eventType, NotificationSeverity.Info, notification.SchemeCode, notification.SourceCode)))
            {
                notification.AddDelivery(subscription, now);
            }

            context.Notifications.Add(notification);
            report.RecordRun(now);
        }

        await context.SaveChangesAsync(cancellationToken);
        return reports;
    }
}

public sealed class RunDueScheduledReportsCommandHandler(
    INotificationsDbContext context,
    ScheduledReportRunner runner,
    IClock clock)
    : IRequestHandler<RunDueScheduledReportsCommand, Result<IReadOnlyList<ScheduledReportDto>>>
{
    public async Task<Result<IReadOnlyList<ScheduledReportDto>>> HandleAsync(
        RunDueScheduledReportsCommand request,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var due = await context.ScheduledReports
            .Where(report => report.IsEnabled && report.NextRunAtUtc != null && report.NextRunAtUtc <= now)
            .ToListAsync(cancellationToken);

        var ran = await runner.RunAsync(due, cancellationToken);
        return Result.Success<IReadOnlyList<ScheduledReportDto>>([.. ran.Select(item => item.ToDto())]);
    }
}

public sealed class RunScheduledReportCommandHandler(INotificationsDbContext context, ScheduledReportRunner runner)
    : IRequestHandler<RunScheduledReportCommand, Result<ScheduledReportDto>>
{
    public async Task<Result<ScheduledReportDto>> HandleAsync(
        RunScheduledReportCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var report = await context.ScheduledReports
            .FirstOrDefaultAsync(item => item.Code == code, cancellationToken);

        if (report is null)
        {
            return Result.Failure<ScheduledReportDto>(ScheduleErrors.NotFound(request.Code));
        }

        await runner.RunAsync([report], cancellationToken);
        return report.ToDto();
    }
}
