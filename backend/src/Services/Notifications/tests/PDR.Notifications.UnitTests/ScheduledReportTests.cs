using AwesomeAssertions;
using PDR.Notifications.Domain.Schedules;
using PDR.Notifications.Domain.Subscriptions;

namespace PDR.Notifications.UnitTests;

public sealed class ScheduledReportTests
{
    // Wednesday.
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_daily_report_lands_at_the_next_occurrence_of_its_hour()
    {
        var report = Create(ScheduleFrequency.Daily, hourUtc: 7).Value;

        report.NextRunAtUtc.Should().Be(new DateTimeOffset(2026, 4, 2, 7, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void A_weekly_report_lands_on_its_day_of_week()
    {
        var report = Create(ScheduleFrequency.Weekly, hourUtc: 6, dayOfWeek: (int)DayOfWeek.Monday).Value;

        report.NextRunAtUtc.Should().Be(new DateTimeOffset(2026, 4, 6, 6, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void A_monthly_report_rolls_to_next_month_once_the_day_has_passed()
    {
        var report = Create(ScheduleFrequency.Monthly, hourUtc: 6, dayOfMonth: 1).Value;

        report.NextRunAtUtc.Should().Be(new DateTimeOffset(2026, 5, 1, 6, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Running_moves_the_schedule_forward_instead_of_replaying_missed_windows()
    {
        var report = Create(ScheduleFrequency.Daily, hourUtc: 7).Value;

        report.RecordRun(Now.AddDays(3));

        report.RunCount.Should().Be(1);
        report.LastRunAtUtc.Should().Be(Now.AddDays(3));
        report.NextRunAtUtc.Should().Be(new DateTimeOffset(2026, 4, 5, 7, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void A_disabled_report_is_never_due()
    {
        var report = Create(ScheduleFrequency.Daily, hourUtc: 7).Value.SetEnabled(false, Now);

        report.NextRunAtUtc.Should().BeNull();
        report.IsDue(Now.AddYears(1)).Should().BeFalse();
    }

    [Theory]
    [InlineData(ScheduleFrequency.Daily, 24, 0, 1, "SCHEDULE.INVALID_HOUR")]
    [InlineData(ScheduleFrequency.Weekly, 6, 9, 1, "SCHEDULE.INVALID_DAY_OF_WEEK")]
    [InlineData(ScheduleFrequency.Monthly, 6, 0, 31, "SCHEDULE.INVALID_DAY_OF_MONTH")]
    public void Impossible_schedules_are_rejected(
        ScheduleFrequency frequency,
        int hourUtc,
        int dayOfWeek,
        int dayOfMonth,
        string expectedCode)
    {
        var result = Create(frequency, hourUtc, dayOfWeek, dayOfMonth);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
    }

    private static PDR.BuildingBlocks.Core.Results.Result<ScheduledReport> Create(
        ScheduleFrequency frequency,
        int hourUtc,
        int dayOfWeek = 1,
        int dayOfMonth = 1) =>
        ScheduledReport.Create(
            "EXEC-DAILY",
            "Executive daily",
            "executive",
            null,
            null,
            frequency,
            hourUtc,
            dayOfWeek,
            dayOfMonth,
            "ops@example.com",
            "tester",
            Now);
}
