using AwesomeAssertions;
using PDR.Simulation.Domain.Testing;

namespace PDR.Simulation.UnitTests;

public sealed class TestPlanTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Empty_plan_cannot_be_activated()
    {
        var plan = TestPlan.Create("UAT", "UAT", "Test Manager", null, null);

        plan.Activate().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Failed_execution_requires_a_defect_reference()
    {
        var plan = ActivePlan();

        var withoutDefect = plan.RecordExecution("TC-1", TestExecutionStatus.Failed, "Rejected", null, null, "tester", Now);
        var withDefect = plan.RecordExecution("TC-1", TestExecutionStatus.Failed, "Rejected", null, "JIRA-1", "tester", Now);

        withoutDefect.IsFailure.Should().BeTrue();
        withoutDefect.Error.Code.Should().Be("TESTPLAN.DEFECT_REQUIRED");
        withDefect.IsSuccess.Should().BeTrue();
        plan.OpenDefectCount.Should().Be(1);
    }

    [Fact]
    public void Plan_only_closes_once_failures_have_a_passing_retest()
    {
        var plan = ActivePlan();
        plan.RecordExecution("TC-1", TestExecutionStatus.Failed, "Rejected", null, "JIRA-1", "tester", Now);

        plan.Close().IsFailure.Should().BeTrue();

        plan.RecordExecution("TC-1", TestExecutionStatus.Passed, "Accepted after fix", "evidence://1", null, "tester", Now);

        plan.OpenDefectCount.Should().Be(0);
        plan.Close().IsSuccess.Should().BeTrue();
        plan.Cases.Single().ExecutionCount.Should().Be(2);
        plan.Cases.Single().DefectReference.Should().Be("JIRA-1");
    }

    [Fact]
    public void Risk_weighted_coverage_counts_a_critical_case_far_above_a_low_one()
    {
        var plan = TestPlan.Create("UAT", "UAT", "Test Manager", null, null);
        plan.AddCase("TC-CRIT", "Critical", TestRisk.Critical, null, null, "Accepted");
        plan.AddCase("TC-LOW", "Low", TestRisk.Low, null, null, "Accepted");
        plan.Activate();

        plan.RecordExecution("TC-LOW", TestExecutionStatus.Passed, "Accepted", null, null, "tester", Now);
        var lowOnly = plan.RiskWeightedCoveragePercent;

        plan.RecordExecution("TC-CRIT", TestExecutionStatus.Passed, "Accepted", null, null, "tester", Now);

        lowOnly.Should().Be(11.11m);
        plan.RiskWeightedCoveragePercent.Should().Be(100m);
    }

    [Fact]
    public void Uat_reconciliation_flags_a_divergence_between_engine_and_platform()
    {
        var plan = ActivePlan();

        plan.RecordUatOutcome("TC-1", "Rejected", "Accepted", "Engine applies a stricter town rule.", Now);

        var testCase = plan.Cases.Single();
        testCase.UatOutcome.Should().Be(UatOutcome.Mismatch);
        testCase.EngineOutcome.Should().Be("Rejected");

        plan.RecordUatOutcome("TC-1", "accepted", "Accepted", null, Now);

        testCase.UatOutcome.Should().Be(UatOutcome.Match);
    }

    private static TestPlan ActivePlan()
    {
        var plan = TestPlan.Create("UAT", "UAT", "Test Manager", "SEPA samples", null);
        plan.AddCase("TC-1", "Structured address accepted", TestRisk.High, "BASE-FUTURE", "SAMPLE-1", "Accepted");
        plan.Activate();
        return plan;
    }
}
