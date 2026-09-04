using AwesomeAssertions;
using PDR.Rules.Domain.Rulesets;

namespace PDR.Rules.UnitTests.Domain;

public sealed class RulesetTests
{
    private static Ruleset SepaRuleset()
    {
        var ruleset = Ruleset.Create("sepa", "SEPA address rules", null);
        ruleset.AddRule(
            1,
            "ADDR.COUNTRY_REQUIRED",
            "Country",
            RuleKind.Required,
            RuleSeverity.Error,
            RuleApplicability.Both,
            "Country is required.",
            null);
        return ruleset;
    }

    [Fact]
    public void Create_normalises_the_scheme_code()
    {
        Ruleset.Create("sepa", "SEPA", null).SchemeCode.Should().Be("SEPA");
    }

    [Fact]
    public void Duplicate_rule_codes_are_rejected_within_a_version()
    {
        var ruleset = SepaRuleset();

        var result = ruleset.AddRule(
            1,
            "ADDR.COUNTRY_REQUIRED",
            "Country",
            RuleKind.Required,
            RuleSeverity.Error,
            RuleApplicability.Both,
            "Duplicate.",
            null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RulesetErrors.DuplicateRuleCode("ADDR.COUNTRY_REQUIRED"));
    }

    [Theory]
    [InlineData(RuleKind.MaxLength, "not-a-number")]
    [InlineData(RuleKind.MaxLength, null)]
    [InlineData(RuleKind.Pattern, "[unclosed")]
    [InlineData(RuleKind.AllowedValues, "")]
    public void Rules_with_unusable_parameters_are_rejected(RuleKind kind, string? parameter)
    {
        var result = SepaRuleset().AddRule(
            1,
            "ADDR.CHECK",
            "Town",
            kind,
            RuleSeverity.Error,
            RuleApplicability.Current,
            "Invalid.",
            parameter);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void A_version_cannot_be_activated_without_rules()
    {
        var ruleset = Ruleset.Create("SEPA", "SEPA", null);

        var result = ruleset.Activate(1, new DateOnly(2026, 1, 1), "alice", DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RulesetErrors.NoRules);
    }

    [Fact]
    public void Activation_stamps_the_actor_and_raises_an_event()
    {
        var ruleset = SepaRuleset();
        var activatedAt = DateTimeOffset.UtcNow;

        var result = ruleset.Activate(1, new DateOnly(2026, 1, 1), "alice", activatedAt);

        result.IsSuccess.Should().BeTrue();
        ruleset.ActiveVersion.Should().NotBeNull();
        ruleset.ActiveVersion!.VersionNumber.Should().Be(1);
        ruleset.ActiveVersion.ActivatedBy.Should().Be("alice");
        ruleset.ActiveVersion.EffectiveFrom.Should().Be(new DateOnly(2026, 1, 1));
        ruleset.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RulesetVersionActivated>();
    }

    [Fact]
    public void An_activated_version_can_no_longer_be_edited()
    {
        var ruleset = SepaRuleset();
        ruleset.Activate(1, new DateOnly(2026, 1, 1), "alice", DateTimeOffset.UtcNow);

        var result = ruleset.AddRule(
            1,
            "ADDR.TOWN_REQUIRED",
            "Town",
            RuleKind.Required,
            RuleSeverity.Error,
            RuleApplicability.Both,
            "Town is required.",
            null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RulesetErrors.VersionIsImmutable);
    }

    [Fact]
    public void A_new_version_copies_the_rules_of_an_earlier_one_and_retires_it_on_activation()
    {
        var ruleset = SepaRuleset();
        ruleset.Activate(1, new DateOnly(2026, 1, 1), "alice", DateTimeOffset.UtcNow);

        var version = ruleset.AddVersion(1, "copy of v1");
        version.IsSuccess.Should().BeTrue();
        version.Value.VersionNumber.Should().Be(2);
        version.Value.Rules.Should().ContainSingle().Which.Code.Should().Be("ADDR.COUNTRY_REQUIRED");

        var effectiveFrom = new DateOnly(2026, 11, 15);
        ruleset.Activate(2, effectiveFrom, "bob", DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();

        ruleset.ActiveVersion!.VersionNumber.Should().Be(2);

        var previous = ruleset.Versions.Single(candidate => candidate.VersionNumber == 1);
        previous.Status.Should().Be(RulesetStatus.Retired);
        previous.EffectiveTo.Should().Be(effectiveFrom);
    }

    [Fact]
    public void A_retired_version_can_be_reactivated_as_a_rollback()
    {
        var ruleset = SepaRuleset();
        ruleset.Activate(1, new DateOnly(2026, 1, 1), "alice", DateTimeOffset.UtcNow);
        ruleset.AddVersion(1, null);
        ruleset.Activate(2, new DateOnly(2026, 11, 15), "bob", DateTimeOffset.UtcNow);

        var rollback = ruleset.Activate(1, new DateOnly(2026, 12, 1), "carol", DateTimeOffset.UtcNow);

        rollback.IsSuccess.Should().BeTrue();
        ruleset.ActiveVersion!.VersionNumber.Should().Be(1);
        ruleset.Versions.Single(version => version.VersionNumber == 2).Status.Should().Be(RulesetStatus.Retired);
    }

    [Fact]
    public void Reactivating_the_current_version_is_rejected()
    {
        var ruleset = SepaRuleset();
        ruleset.Activate(1, new DateOnly(2026, 1, 1), "alice", DateTimeOffset.UtcNow);

        var result = ruleset.Activate(1, new DateOnly(2026, 2, 1), "alice", DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RulesetErrors.VersionAlreadyActive);
    }
}
