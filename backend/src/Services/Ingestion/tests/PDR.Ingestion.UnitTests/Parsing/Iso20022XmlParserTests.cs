using System.Text;
using AwesomeAssertions;
using PDR.Ingestion.Application.Parsing;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.UnitTests.Parsing;

public sealed class Iso20022XmlParserTests
{
    private const string Pain001 =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:pain.001.001.09">
          <CstmrCdtTrfInitn>
            <GrpHdr><MsgId>MSG-1</MsgId></GrpHdr>
            <PmtInf>
              <Dbtr>
                <Nm>Debtor Ltd</Nm>
                <PstlAdr><StrtNm>High Street</StrtNm><BldgNb>10</BldgNb><PstCd>SW1A 1AA</PstCd><TwnNm>London</TwnNm><Ctry>GB</Ctry></PstlAdr>
              </Dbtr>
              <CdtTrfTxInf>
                <PmtId><EndToEndId>E2E-1</EndToEndId></PmtId>
                <Cdtr>
                  <Nm>Creditor SA</Nm>
                  <PstlAdr><AdrLine>12 Rue de Rivoli</AdrLine><AdrLine>75001 Paris</AdrLine></PstlAdr>
                </Cdtr>
              </CdtTrfTxInf>
            </PmtInf>
          </CstmrCdtTrfInitn>
        </Document>
        """;

    private static ParseOutcome Parse(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new Iso20022XmlParser().Parse(stream, new ParserOptions(",", 1000));
    }

    [Fact]
    public void Structured_and_unstructured_parties_are_both_extracted()
    {
        var outcome = Parse(Pain001);

        outcome.Addresses.Should().HaveCount(2);

        var debtor = outcome.Addresses.Single(address => address.PartyRole == PartyRole.Debtor);
        debtor.StreetName.Should().Be("High Street");
        debtor.BuildingNumber.Should().Be("10");
        debtor.TownName.Should().Be("London");
        debtor.Country.Should().Be("GB");
        debtor.MessageId.Should().Be("MSG-1");

        var creditor = outcome.Addresses.Single(address => address.PartyRole == PartyRole.Creditor);
        creditor.AddressLines.Should().Be("12 Rue de Rivoli\n75001 Paris");
        creditor.StreetName.Should().BeNull();
        creditor.EndToEndId.Should().Be("E2E-1");
    }

    [Fact]
    public void A_party_without_a_postal_address_is_still_reported()
    {
        var outcome = Parse(
            """
            <Document xmlns="urn:iso:std:iso:20022:tech:xsd:pacs.008.001.08">
              <FIToFICstmrCdtTrf>
                <GrpHdr><MsgId>MSG-2</MsgId></GrpHdr>
                <CdtTrfTxInf><Cdtr><Nm>No Address Ltd</Nm></Cdtr></CdtTrfTxInf>
              </FIToFICstmrCdtTrf>
            </Document>
            """);

        var address = outcome.Addresses.Should().ContainSingle().Subject;
        address.PartyName.Should().Be("No Address Ltd");
        address.Country.Should().BeNull();
        address.AddressLines.Should().BeNull();
    }

    [Fact]
    public void A_document_type_definition_is_refused_rather_than_expanded()
    {
        var parse = () => Parse(
            """
            <?xml version="1.0"?>
            <!DOCTYPE Document [<!ENTITY bomb "boom">]>
            <Document><Cdtr><Nm>&bomb;</Nm></Cdtr></Document>
            """);

        parse.Should().Throw<ParserException>();
    }

    [Fact]
    public void A_message_without_any_party_is_rejected()
    {
        var parse = () => Parse("<Document><GrpHdr><MsgId>MSG-3</MsgId></GrpHdr></Document>");

        parse.Should().Throw<ParserException>().WithMessage("*No debtor or creditor parties*");
    }

    [Fact]
    public void Malformed_xml_is_reported_as_a_parse_failure()
    {
        var parse = () => Parse("<Document><Cdtr>");

        parse.Should().Throw<ParserException>().WithMessage("*not well-formed*");
    }
}
