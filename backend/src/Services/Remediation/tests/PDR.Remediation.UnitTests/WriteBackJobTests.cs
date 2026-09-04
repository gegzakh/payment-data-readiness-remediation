using AwesomeAssertions;
using PDR.Remediation.Domain.Cases;
using PDR.Remediation.Domain.WriteBack;

namespace PDR.Remediation.UnitTests;

/// <summary>Write-back accounting: nothing silently succeeds and nothing is irreversible (FR-WB-005 to FR-WB-008).</summary>
public sealed class WriteBackJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_partly_stale_run_is_reported_as_partially_failed()
    {
        var job = Job();
        var applied = job.AddItem(Guid.NewGuid(), "REC-1", "v1", "before", "after");
        var stale = job.AddItem(Guid.NewGuid(), "REC-2", "v1", "before", "after");

        applied.Apply("corr-1", Now);
        stale.MarkStale("v2");
        job.CompleteApply(Now);

        job.Status.Should().Be(WriteBackStatus.PartiallyFailed);
        job.FailureSummary.Should().Contain("stale");
        job.CountsReconcile().Should().BeTrue();
        stale.Message.Should().Contain("v2");
    }

    [Fact]
    public void An_item_left_pending_breaks_reconciliation()
    {
        var job = Job();
        job.AddItem(Guid.NewGuid(), "REC-1", "v1", "before", "after").Apply("corr-1", Now);
        job.AddItem(Guid.NewGuid(), "REC-2", "v1", "before", "after");

        job.CompleteApply(Now);

        job.CountsReconcile().Should().BeFalse();
    }

    [Fact]
    public void Read_after_write_confirmation_closes_the_job()
    {
        var job = Job();
        var item = job.AddItem(Guid.NewGuid(), "REC-1", "v1", "before", "after");
        item.Apply("corr-1", Now);
        job.CompleteApply(Now);

        job.Confirm([item.Id], Now).IsSuccess.Should().BeTrue();

        job.Status.Should().Be(WriteBackStatus.Confirmed);
        job.ConfirmedCount.Should().Be(1);
    }

    [Fact]
    public void A_job_that_was_never_applied_cannot_be_confirmed_or_rolled_back()
    {
        var job = Job();
        job.AddItem(Guid.NewGuid(), "REC-1", "v1", "before", "after");

        job.Confirm([], Now).Error.Code.Should().Be("WRITEBACK.NOT_APPLIED");
        job.Rollback("no", Now).Error.Code.Should().Be("WRITEBACK.NOT_ROLLBACKABLE");
    }

    [Fact]
    public void Rollback_reverses_only_the_items_that_reached_the_source()
    {
        var job = Job();
        var applied = job.AddItem(Guid.NewGuid(), "REC-1", "v1", "before", "after");
        var failed = job.AddItem(Guid.NewGuid(), "REC-2", "v1", "before", "after");
        applied.Apply("corr-1", Now);
        failed.Fail("target rejected");
        job.CompleteApply(Now);

        job.Rollback("Wrong reference data", Now).IsSuccess.Should().BeTrue();

        job.Status.Should().Be(WriteBackStatus.RolledBack);
        applied.Status.Should().Be(WriteBackItemStatus.RolledBack);
        failed.Status.Should().Be(WriteBackItemStatus.Failed);
    }

    [Fact]
    public void A_target_only_accepts_the_fields_it_declares()
    {
        var target = WriteBackTarget.Create(
            "cbs",
            WriteBackMode.Api,
            "Country,Town,PostCode",
            "http://localhost/cbs",
            null,
            "Sun 02:00-04:00 UTC",
            500,
            requiresApproval: true,
            "Restore the stored original value");

        target.SourceCode.Should().Be("CBS");
        target.Allows("postcode").Should().BeTrue();
        target.Allows("street").Should().BeFalse();
    }

    private static WriteBackJob Job() =>
        WriteBackJob.Create("CBS", WriteBackMode.Api, "key-1", "operator", Now);
}
