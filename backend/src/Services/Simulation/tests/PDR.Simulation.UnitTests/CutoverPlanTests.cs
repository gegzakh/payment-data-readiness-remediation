using AwesomeAssertions;
using PDR.Simulation.Domain.Cutover;

namespace PDR.Simulation.UnitTests;

public sealed class CutoverPlanTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Marking_a_criterion_met_requires_evidence_and_waiving_requires_a_rationale()
    {
        var plan = PlanWithCriteria();

        var metWithout = plan.RecordCriterionStatus("ENTRY-1", CriterionStatus.Met, null, null, "owner", Now);
        var waivedWithout = plan.RecordCriterionStatus("ENTRY-1", CriterionStatus.Waived, null, null, "owner", Now);
        var met = plan.RecordCriterionStatus("ENTRY-1", CriterionStatus.Met, "evidence://run/1", null, "owner", Now);

        metWithout.Error.Code.Should().Be("CUTOVER.EVIDENCE_REQUIRED");
        waivedWithout.Error.Code.Should().Be("CUTOVER.RATIONALE_REQUIRED");
        met.IsSuccess.Should().BeTrue();

        var criterion = plan.Criteria.Single(item => item.Reference == "ENTRY-1");
        criterion.EvidenceReference.Should().Be("evidence://run/1");
        criterion.RecordedBy.Should().Be("owner");
    }

    [Fact]
    public void Recommendation_is_go_only_when_every_criterion_is_met_and_nothing_is_outstanding()
    {
        var plan = PlanWithCriteria();
        MeetAll(plan);

        plan.Recommend(residualExposure: 0, openDefects: 0, expiredExceptions: 0)
            .Should().Be(GoNoGoRecommendation.Go);
    }

    [Theory]
    [InlineData(5, 0, 0, GoNoGoRecommendation.NoGo)]
    [InlineData(0, 0, 2, GoNoGoRecommendation.NoGo)]
    [InlineData(0, 3, 0, GoNoGoRecommendation.GoWithConditions)]
    public void Residual_exposure_expired_exceptions_and_defects_drive_the_recommendation(
        int residualExposure,
        int openDefects,
        int expiredExceptions,
        GoNoGoRecommendation expected)
    {
        var plan = PlanWithCriteria();
        MeetAll(plan);

        plan.Recommend(residualExposure, openDefects, expiredExceptions).Should().Be(expected);
    }

    [Fact]
    public void A_failed_blocking_criterion_forces_no_go_and_blocks_approval()
    {
        var plan = PlanWithCriteria();
        MeetAll(plan);
        plan.RecordCriterionStatus("ENTRY-1", CriterionStatus.Failed, null, null, "owner", Now);

        var recommendation = plan.Recommend(0, 0, 0);
        var approval = plan.Approve("Programme", "approver", ApprovalDecision.Approved, "Looks fine", recommendation, Now);

        recommendation.Should().Be(GoNoGoRecommendation.NoGo);
        approval.IsFailure.Should().BeTrue();
        approval.Error.Code.Should().Be("CUTOVER.CANNOT_APPROVE_NO_GO");
    }

    [Fact]
    public void A_waived_criterion_downgrades_the_recommendation_to_go_with_conditions()
    {
        var plan = PlanWithCriteria();
        MeetAll(plan);
        plan.RecordCriterionStatus("ENTRY-2", CriterionStatus.Waived, null, "Accepted by risk committee", "owner", Now);

        plan.Recommend(0, 0, 0).Should().Be(GoNoGoRecommendation.GoWithConditions);
    }

    [Fact]
    public void Approving_twice_for_the_same_role_replaces_the_earlier_sign_off()
    {
        var plan = PlanWithCriteria();
        MeetAll(plan);

        plan.Approve("Programme", "first", ApprovalDecision.Approved, "Go", GoNoGoRecommendation.Go, Now);
        plan.Approve("Programme", "second", ApprovalDecision.Rejected, "Changed my mind", GoNoGoRecommendation.Go, Now);

        plan.Approvals.Should().ContainSingle();
        plan.Approvals.Single().Approver.Should().Be("second");
        plan.Recommend(0, 0, 0).Should().Be(GoNoGoRecommendation.NoGo);
    }

    [Fact]
    public void Freeze_window_is_reported_for_dates_inside_it()
    {
        var plan = PlanWithCriteria();
        plan.SetOperationalPlan(new DateOnly(2026, 11, 17), new DateOnly(2026, 11, 24), "Roll back", "Hypercare");

        plan.IsFrozen(new DateOnly(2026, 11, 20)).Should().BeTrue();
        plan.IsFrozen(new DateOnly(2026, 11, 25)).Should().BeFalse();
    }

    private static CutoverPlan PlanWithCriteria()
    {
        var plan = CutoverPlan.Create("CUT", "Cutover", new DateOnly(2026, 11, 22), "Programme");
        plan.AddCriterion("ENTRY-1", CriterionKind.Entry, "Readiness above threshold", "Data Quality", true);
        plan.AddCriterion("ENTRY-2", CriterionKind.Entry, "No expired exceptions", "Compliance", true);
        plan.AddCriterion("EXIT-1", CriterionKind.Exit, "Reject rate back to baseline", "Operations", false);
        return plan;
    }

    private static void MeetAll(CutoverPlan plan)
    {
        foreach (var criterion in plan.Criteria)
        {
            plan.RecordCriterionStatus(criterion.Reference, CriterionStatus.Met, "evidence://x", null, "owner", Now);
        }
    }
}
