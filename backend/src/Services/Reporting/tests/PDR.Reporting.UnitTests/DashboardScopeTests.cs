using AwesomeAssertions;
using PDR.Reporting.Domain.Dashboards;

namespace PDR.Reporting.UnitTests;

public sealed class DashboardScopeTests
{
    [Fact]
    public void Create_normalizes_filters_so_the_same_scope_always_has_the_same_key()
    {
        var first = DashboardScope.Create("sepa, swift", "cbs", null, null, null);
        var second = DashboardScope.Create("SWIFT,SEPA,swift", " CBS ", null, null, null);

        second.Key.Should().Be(first.Key);
        first.SchemeCodes.Should().Be("SEPA,SWIFT");
    }

    [Fact]
    public void Key_separates_scopes_that_differ_only_by_as_of_date()
    {
        var latest = DashboardScope.Create("SEPA", null, null, null, null);
        var dated = DashboardScope.Create("SEPA", null, null, null, new DateOnly(2026, 3, 1));

        dated.Key.Should().NotBe(latest.Key);
    }

    [Fact]
    public void Includes_admits_everything_when_no_filter_is_set()
    {
        DashboardScope.All.Includes("Scheme", "SEPA").Should().BeTrue();
        DashboardScope.All.Includes("Source", "CBS").Should().BeTrue();
    }

    [Fact]
    public void Includes_applies_the_matching_dimension_filter_only()
    {
        var scope = DashboardScope.Create("SEPA", null, null, null, null);

        scope.Includes("Scheme", "SEPA").Should().BeTrue();
        scope.Includes("Scheme", "SWIFT").Should().BeFalse();
        scope.Includes("Source", "CBS").Should().BeTrue();
    }

    [Fact]
    public void Includes_rejects_explicitly_excluded_keys()
    {
        var scope = DashboardScope.Create(null, null, null, "LEGACY", null);

        scope.Includes("Source", "LEGACY").Should().BeFalse();
        scope.Includes("Source", "CBS").Should().BeTrue();
    }

    [Fact]
    public void Description_names_the_filters_that_are_in_force()
    {
        var scope = DashboardScope.Create("SEPA", "CBS", null, "LEGACY", null);

        scope.Description.Should().Contain("SEPA").And.Contain("CBS").And.Contain("LEGACY");
    }
}
