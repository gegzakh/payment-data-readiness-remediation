using AwesomeAssertions;
using PDR.Remediation.Application.Cases;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.UnitTests;

/// <summary>
/// The proposal is the maker's starting point, so it has to be reproducible, honest about what it could
/// not resolve, and free of invented data (FR-REM-003, FR-REM-004).
/// </summary>
public sealed class ProposalGeneratorTests
{
    [Fact]
    public void Splits_street_and_building_number_out_of_an_unstructured_address()
    {
        var proposal = ProposalGenerator.Propose(new OriginalAddress(
            null,
            null,
            null,
            null,
            null,
            "Hauptstrasse 12|10115 Berlin|Germany"));

        proposal.Country.Should().Be("DE");
        proposal.StreetName.Should().Be("Hauptstrasse");
        proposal.BuildingNumber.Should().Be("12");
        proposal.PostCode.Should().Be("10115");
        proposal.TownName.Should().Be("Berlin");
    }

    [Fact]
    public void Keeps_values_the_source_already_holds_at_full_confidence()
    {
        var proposal = ProposalGenerator.Propose(new OriginalAddress(
            "de",
            "Berlin",
            "10115",
            "Hauptstrasse",
            "12",
            null));

        proposal.Country.Should().Be("DE");
        proposal.Confidence.Country.Should().Be(100m);
        proposal.Confidence.Town.Should().Be(100m);
        proposal.Confidence.PostCode.Should().Be(100m);
    }

    [Fact]
    public void Is_deterministic_for_the_same_input()
    {
        var original = new OriginalAddress(null, null, null, null, null, "Rue de Rivoli 5|75001 Paris|France");

        var first = ProposalGenerator.Propose(original);
        var second = ProposalGenerator.Propose(original);

        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public void Reports_ambiguity_instead_of_guessing_a_missing_country()
    {
        var proposal = ProposalGenerator.Propose(new OriginalAddress(null, null, null, null, null, "PO Box 44"));

        proposal.Country.Should().BeNull();
        proposal.Confidence.Country.Should().Be(0m);
        proposal.Ambiguity.Should().Contain("country");
    }

    [Fact]
    public void Offers_the_rejected_postal_code_candidates_as_alternatives()
    {
        var proposal = ProposalGenerator.Propose(new OriginalAddress(
            "DE",
            null,
            null,
            null,
            null,
            "Hauptstrasse 12|10115 Berlin 80331"));

        proposal.Alternatives.Should().NotBeNull();
        proposal.Alternatives.Should().Contain("postCode=");
        proposal.Confidence.PostCode.Should().BeLessThan(90m);
    }

    [Fact]
    public void Rejects_a_postal_code_that_does_not_match_the_country_pattern()
    {
        var proposal = ProposalGenerator.Propose(
            new OriginalAddress("FR", null, null, null, null, "Rue de Rivoli 5|750 Paris"),
            new Dictionary<string, string> { ["FR"] = @"^\d{5}$" });

        proposal.PostCode.Should().BeNull();
        proposal.Ambiguity.Should().Contain("postal code");
    }
}
