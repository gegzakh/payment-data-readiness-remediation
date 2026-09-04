using AwesomeAssertions;
using PDR.Remediation.Application.Cases;
using PDR.Remediation.Application.Upstream;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.UnitTests;

/// <summary>
/// The case key decides what counts as one defect. It must fold repeat payments together and still keep
/// distinct parties, roles and sources apart (FR-REM-001).
/// </summary>
public sealed class CaseKeyTests
{
    [Fact]
    public void Two_payments_carrying_the_same_bad_address_share_one_key()
    {
        CaseGenerator.CaseKeyOf(Assessment(messageId: "MSG-1"))
            .Should().Be(CaseGenerator.CaseKeyOf(Assessment(messageId: "MSG-2")));
    }

    [Fact]
    public void Casing_and_padding_of_the_address_do_not_create_a_second_case()
    {
        CaseGenerator.CaseKeyOf(Assessment(town: "Berlin"))
            .Should().Be(CaseGenerator.CaseKeyOf(Assessment(town: "  berlin ")));
    }

    [Fact]
    public void A_different_party_role_or_source_is_a_different_case()
    {
        var creditor = CaseGenerator.CaseKeyOf(Assessment());
        var debtor = CaseGenerator.CaseKeyOf(Assessment(role: PartyRole.Debtor));
        var otherSource = CaseGenerator.CaseKeyOf(Assessment(sourceCode: "CRM"));

        debtor.Should().NotBe(creditor);
        otherSource.Should().NotBe(creditor);
    }

    [Fact]
    public void The_key_is_prefixed_by_the_source_and_carries_no_personal_data()
    {
        var key = CaseGenerator.CaseKeyOf(Assessment());

        key.Should().StartWith("CBS:");
        key.Should().NotContainAny("Acme", "Berlin", "Hauptstrasse");
    }

    private static AssessedAddress Assessment(
        string sourceCode = "cbs",
        string? messageId = "MSG-1",
        string? town = "Berlin",
        PartyRole role = PartyRole.Creditor) =>
        new(
            Guid.NewGuid(),
            sourceCode,
            "SEPA",
            messageId,
            $"E2E-{messageId}",
            role,
            "Acme GmbH",
            "Unstructured",
            "Warning",
            "Rejected",
            "DE",
            town,
            "10115",
            "Hauptstrasse",
            "12",
            null,
            "batch/1",
            [new AssessedIssue("Future", "ADDR-STRUCT-001", "TownName", "Error", "Town is required")]);
}
