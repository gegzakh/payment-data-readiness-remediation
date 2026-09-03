using System.Text;
using AwesomeAssertions;
using PDR.Ingestion.Application.Parsing;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.UnitTests.Parsing;

public sealed class CsvAddressParserTests
{
    private static readonly ParserOptions Options = new(",", 1000);

    private static ParseOutcome Parse(string content, ParserOptions? options = null)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new CsvAddressParser().Parse(stream, options ?? Options);
    }

    [Fact]
    public void Header_columns_may_appear_in_any_order()
    {
        var outcome = Parse(
            """
            Town,PartyRole,Country,PartyName,PostCode
            Berlin,Creditor,DE,Acme GmbH,10115
            """);

        var address = outcome.Addresses.Should().ContainSingle().Subject;
        address.PartyRole.Should().Be(PartyRole.Creditor);
        address.TownName.Should().Be("Berlin");
        address.Country.Should().Be("DE");
        address.PostCode.Should().Be("10115");
    }

    [Fact]
    public void Quoted_fields_may_contain_the_delimiter()
    {
        var outcome = Parse(
            """
            PartyRole,PartyName,AddressLines
            Debtor,"Foo, Bar and Sons","Line 1, 2"
            """);

        outcome.Addresses.Single().PartyName.Should().Be("Foo, Bar and Sons");
        outcome.Addresses.Single().AddressLines.Should().Be("Line 1, 2");
    }

    [Fact]
    public void A_row_with_an_unknown_party_role_is_reported_as_a_failure_not_an_exception()
    {
        var outcome = Parse(
            """
            PartyRole,Country
            Beneficiary,DE
            Debtor,FR
            """);

        outcome.Addresses.Should().ContainSingle();
        outcome.Failures.Should().ContainSingle().Which.Sequence.Should().Be(1);
        outcome.InputRecordCount.Should().Be(2);
    }

    [Fact]
    public void A_row_carrying_no_address_content_is_excluded()
    {
        var outcome = Parse(
            """
            PartyRole,Country,Town
            Debtor,,
            Debtor,DE,Berlin
            """);

        outcome.Addresses.Should().ContainSingle();
        outcome.ExcludedCount.Should().Be(1);
        outcome.InputRecordCount.Should().Be(2);
    }

    [Fact]
    public void A_missing_party_role_column_rejects_the_file()
    {
        var parse = () => Parse(
            """
            Country,Town
            DE,Berlin
            """);

        parse.Should().Throw<ParserException>().WithMessage("*partyrole*");
    }

    [Fact]
    public void An_empty_file_is_rejected()
    {
        var parse = () => Parse(string.Empty);

        parse.Should().Throw<ParserException>();
    }

    [Fact]
    public void The_configured_record_cap_is_enforced()
    {
        var parse = () => Parse(
            """
            PartyRole,Country
            Debtor,DE
            Debtor,FR
            """,
            new ParserOptions(",", 1));

        parse.Should().Throw<ParserException>().WithMessage("*maximum of 1 records*");
    }

    [Fact]
    public void A_configured_semicolon_delimiter_is_honoured()
    {
        var outcome = Parse(
            """
            PartyRole;Country;Town
            Debtor;DE;Berlin
            """,
            new ParserOptions(";", 1000));

        outcome.Addresses.Single().TownName.Should().Be("Berlin");
    }
}
