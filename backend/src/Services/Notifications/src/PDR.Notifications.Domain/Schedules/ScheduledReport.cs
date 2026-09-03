using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.Domain.Schedules;

/// <summary>
/// A dashboard that is delivered on a cadence rather than pulled. The next due time is computed from the
/// schedule itself, so a missed window (service down, paused report) never floods recipients on restart —
/// the report simply lands at the next slot (FR-RPT-004).
/// </summary>
public sealed class ScheduledReport : AggregateRoot
{
    private ScheduledReport()
    {
    }

    private ScheduledReport(
        string code,
        string name,
        string audience,
        string? schemeCodes,
        string? sourceCodes,
        ScheduleFrequency frequency,
        int hourUtc,
        int dayOfWeek,
        int dayOfMonth,
        string recipients,
        string owner)
    {
        Code = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(code).ToUpperInvariant(), 64);
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 140);
        Audience = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(audience).ToLowerInvariant(), 32);
        SchemeCodes = schemeCodes;
        SourceCodes = sourceCodes;
        Frequency = frequency;
        HourUtc = hourUtc;
        DayOfWeek = dayOfWeek;
        DayOfMonth = dayOfMonth;
        Recipients = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(recipients), 1024);
        Owner = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(owner), 140);
        IsEnabled = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>The reporting dashboard key, e.g. <c>executive</c>.</summary>
    public string Audience { get; private set; } = string.Empty;

    public string? SchemeCodes { get; private set; }

    public string? SourceCodes { get; private set; }

    public ScheduleFrequency Frequency { get; private set; }

    public int HourUtc { get; private set; }

    /// <summary>Used by weekly schedules; 0 = Sunday.</summary>
    public int DayOfWeek { get; private set; }

    /// <summary>Used by monthly schedules; clamped to the length of the target month.</summary>
    public int DayOfMonth { get; private set; }

    public string Recipients { get; private set; } = string.Empty;

    public string Owner { get; private set; } = string.Empty;

    public bool IsEnabled { get; private set; }

    public DateTimeOffset? LastRunAtUtc { get; private set; }

    public DateTimeOffset? NextRunAtUtc { get; private set; }

    public int RunCount { get; private set; }

    public static Result<ScheduledReport> Create(
        string code,
        string name,
        string audience,
        string? schemeCodes,
        string? sourceCodes,
        ScheduleFrequency frequency,
        int hourUtc,
        int dayOfWeek,
        int dayOfMonth,
        string recipients,
        string owner,
        DateTimeOffset now)
    {
        if (hourUtc is < 0 or > 23)
        {
            return Result.Failure<ScheduledReport>(ScheduleErrors.InvalidHour(hourUtc));
        }

        if (frequency == ScheduleFrequency.Weekly && dayOfWeek is < 0 or > 6)
        {
            return Result.Failure<ScheduledReport>(ScheduleErrors.InvalidDayOfWeek(dayOfWeek));
        }

        if (frequency == ScheduleFrequency.Monthly && dayOfMonth is < 1 or > 28)
        {
            return Result.Failure<ScheduledReport>(ScheduleErrors.InvalidDayOfMonth(dayOfMonth));
        }

        var report = new ScheduledReport(
            code,
            name,
            audience,
            schemeCodes,
            sourceCodes,
            frequency,
            hourUtc,
            dayOfWeek,
            dayOfMonth,
            recipients,
            owner);

        report.NextRunAtUtc = report.ComputeNextRun(now);
        return report;
    }

    public ScheduledReport SetEnabled(bool enabled, DateTimeOffset now)
    {
        IsEnabled = enabled;
        NextRunAtUtc = enabled ? ComputeNextRun(now) : null;
        return this;
    }

    public bool IsDue(DateTimeOffset now) => IsEnabled && NextRunAtUtc is not null && NextRunAtUtc <= now;

    public ScheduledReport RecordRun(DateTimeOffset now)
    {
        LastRunAtUtc = now;
        RunCount++;
        NextRunAtUtc = ComputeNextRun(now);
        return this;
    }

    /// <summary>The first slot strictly after <paramref name="from"/> that matches the schedule.</summary>
    public DateTimeOffset ComputeNextRun(DateTimeOffset from)
    {
        var reference = from.ToUniversalTime();

        return Frequency switch
        {
            ScheduleFrequency.Daily => NextDaily(reference),
            ScheduleFrequency.Weekly => NextWeekly(reference),
            _ => NextMonthly(reference)
        };
    }

    private DateTimeOffset NextDaily(DateTimeOffset reference)
    {
        var candidate = Slot(reference.Date);
        return candidate > reference ? candidate : Slot(reference.Date.AddDays(1));
    }

    private DateTimeOffset NextWeekly(DateTimeOffset reference)
    {
        var delta = ((DayOfWeek - (int)reference.DayOfWeek) + 7) % 7;
        var candidate = Slot(reference.Date.AddDays(delta));
        return candidate > reference ? candidate : Slot(reference.Date.AddDays(delta + 7));
    }

    private DateTimeOffset NextMonthly(DateTimeOffset reference)
    {
        var candidate = Slot(new DateTime(reference.Year, reference.Month, DayOfMonth, 0, 0, 0, DateTimeKind.Utc));
        if (candidate > reference)
        {
            return candidate;
        }

        var next = new DateTime(reference.Year, reference.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        return Slot(new DateTime(next.Year, next.Month, DayOfMonth, 0, 0, 0, DateTimeKind.Utc));
    }

    private DateTimeOffset Slot(DateTime day) =>
        new(DateTime.SpecifyKind(day.Date.AddHours(HourUtc), DateTimeKind.Utc));
}

public static class ScheduleErrors
{
    public static Error InvalidHour(int hour) =>
        Error.Validation("SCHEDULE.INVALID_HOUR", $"'{hour}' is not an hour of the day in UTC.");

    public static Error InvalidDayOfWeek(int day) =>
        Error.Validation("SCHEDULE.INVALID_DAY_OF_WEEK", $"'{day}' is not a day of the week (0 = Sunday).");

    public static Error InvalidDayOfMonth(int day) =>
        Error.Validation(
            "SCHEDULE.INVALID_DAY_OF_MONTH",
            $"'{day}' is not a usable day of the month; use 1-28 so every month has the slot.");

    public static Error Duplicate(string code) =>
        Error.Conflict("SCHEDULE.DUPLICATE", $"A scheduled report with code '{code}' already exists.");

    public static Error NotFound(string code) =>
        Error.NotFound("SCHEDULE.NOT_FOUND", $"Scheduled report '{code}' was not found.");
}
