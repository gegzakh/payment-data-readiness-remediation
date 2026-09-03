using AwesomeAssertions;
using PDR.Validation.Application.Assess;
using PDR.Validation.Application.Upstream;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.UnitTests;

public class RuleEvaluatorTests
{
    private static AddressSnapshot Address(
        string? country = "DE",
        string? townName = "Berlin",
        string? postCode = "10115",
        string? streetName = "Invalidenstrasse",
        string? buildingNumber = "12",
        string? addressLines = null,
        AddressClassification classification = AddressClassification.Structured) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CORE",
            1,
            "MSG-1",
            "E2E-1",
            PartyRole.Creditor,
            "Acme GmbH",
            country,
            townName,
            postCode,
            streetName,
            buildingNumber,
            addressLines,
            "SEPA",
            false,
            classification);

    private static RuleSnapshot Rule(
        RuleCheck kind,
        string field = "Country",
        string? parameter = null,
        IssueSeverity severity = IssueSeverity.Error) =>
        new($"ADDR.{kind}", field, kind, severity, $"{field} violates {kind}.", parameter);

    [Fact]
    public void Reports_required_field_when_missing()
    {
        var findings = RuleEvaluator.Evaluate(Address(townName: null), [Rule(RuleCheck.Required, "TownName")]);

        findings.Should().ContainSingle();
        findings[0].Actual.Should().Be("missing");
        findings[0].Severity.Should().Be(IssueSeverity.Error);
    }

    [Fact]
    public void Accepts_present_required_field()
    {
        RuleEvaluator.Evaluate(Address(), [Rule(RuleCheck.Required, "TownName")]).Should().BeEmpty();
    }

    [Fact]
    public void Reports_value_longer_than_the_maximum()
    {
        var findings = RuleEvaluator.Evaluate(
            Address(townName: new string('x', 40)),
            [Rule(RuleCheck.MaxLength, "TownName", "35")]);

        findings.Should().ContainSingle().Which.Expected.Should().Be("at most 35 characters");
    }

    [Fact]
    public void Reports_value_not_matching_the_pattern()
    {
        var findings = RuleEvaluator.Evaluate(
            Address(postCode: "abc"),
            [Rule(RuleCheck.Pattern, "PostCode", "^[0-9]{5}$")]);

        findings.Should().ContainSingle().Which.Actual.Should().Be("abc");
    }

    [Fact]
    public void Ignores_a_malformed_pattern_instead_of_failing_the_run()
    {
        var findings = RuleEvaluator.Evaluate(Address(), [Rule(RuleCheck.Pattern, "PostCode", "([unclosed")]);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Reports_value_outside_the_allowed_list()
    {
        var findings = RuleEvaluator.Evaluate(Address(country: "ZZ"), [Rule(RuleCheck.AllowedValues, "Country", "DE, FR")]);

        findings.Should().ContainSingle();
        RuleEvaluator.Evaluate(Address(country: "fr"), [Rule(RuleCheck.AllowedValues, "Country", "DE, FR")])
            .Should().BeEmpty();
    }

    [Fact]
    public void Reports_prohibited_content()
    {
        var findings = RuleEvaluator.Evaluate(
            Address(streetName: "PO BOX 42"),
            [Rule(RuleCheck.Prohibited, "StreetName", "PO BOX")]);

        findings.Should().ContainSingle();
    }

    [Theory]
    [InlineData(AddressClassification.Unstructured, 1)]
    [InlineData(AddressClassification.Hybrid, 1)]
    [InlineData(AddressClassification.Structured, 0)]
    public void Reports_unstructured_and_hybrid_addresses_under_structured_only(
        AddressClassification classification,
        int expected)
    {
        var findings = RuleEvaluator.Evaluate(
            Address(classification: classification),
            [Rule(RuleCheck.StructuredOnly, "AddressLine")]);

        findings.Should().HaveCount(expected);
    }

    [Fact]
    public void Ignores_rules_addressing_an_unknown_field()
    {
        RuleEvaluator.Evaluate(Address(), [Rule(RuleCheck.Required, "DepartmentName")])
            .Should().ContainSingle("an unresolvable field reads as missing rather than silently passing");
    }
}
