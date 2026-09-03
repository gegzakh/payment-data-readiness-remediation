using AwesomeAssertions;
using PDR.Simulation.Application.Scenarios;
using PDR.Simulation.Domain.Scenarios;

namespace PDR.Simulation.UnitTests;

public sealed class ScenarioTests
{
    [Fact]
    public void Create_normalises_scope_filters_so_equivalent_scopes_are_identical()
    {
        var first = Scenario.Create(
            "s1",
            "First",
            ScenarioMode.Future,
            new DateOnly(2026, 11, 22),
            schemeCodes: " sepa , swift ,SEPA",
            countries: "de,at");

        var second = Scenario.Create(
            "s2",
            "Second",
            ScenarioMode.Future,
            new DateOnly(2026, 11, 22),
            schemeCodes: "SWIFT,SEPA",
            countries: " AT,DE ");

        first.SchemeCodes.Should().Be("SEPA,SWIFT");
        first.Countries.Should().Be(second.Countries);
        first.Code.Should().Be("S1");
    }

    [Fact]
    public void Locked_scenario_cannot_be_edited_but_can_still_run()
    {
        var scenario = Scenario.Create("BASE", "Base", ScenarioMode.Current, new DateOnly(2026, 1, 1));

        scenario.Lock().IsSuccess.Should().BeTrue();

        var update = scenario.Update("Renamed", new DateOnly(2026, 2, 1), null, null, null, null, null, null, null);

        update.IsFailure.Should().BeTrue();
        scenario.Name.Should().Be("Base");
        scenario.IsRunnable.Should().BeTrue();
    }

    [Fact]
    public void Archived_scenario_is_not_runnable()
    {
        var scenario = Scenario.Create("BASE", "Base", ScenarioMode.Current, new DateOnly(2026, 1, 1));

        scenario.Archive().IsSuccess.Should().BeTrue();

        scenario.IsRunnable.Should().BeFalse();
    }

    [Fact]
    public void Run_key_is_stable_for_the_same_definition_and_changes_with_the_ruleset()
    {
        var scenario = Scenario.Create(
            "BASE",
            "Base",
            ScenarioMode.Future,
            new DateOnly(2026, 11, 22),
            schemeCodes: "SEPA");

        var first = SimulationRunner.BuildRunKey(scenario, "2026.1");
        var second = SimulationRunner.BuildRunKey(scenario, "2026.1");
        var moved = SimulationRunner.BuildRunKey(scenario, "2026.2");

        second.Should().Be(first);
        moved.Should().NotBe(first);
        first.Should().StartWith("BASE:");
    }
}
