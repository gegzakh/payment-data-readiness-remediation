using System.Xml;
using System.Xml.Linq;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Application.Parsing;

/// <summary>
/// Parses ISO 20022 payment messages (pain.001, pacs.008 and siblings) by element local name, so any
/// message version is handled (FR-ING-002). The reader prohibits DTDs and external resolution and caps
/// entity expansion, which is what makes an untrusted upload safe to parse (FR-ING-003).
/// </summary>
public sealed class Iso20022XmlParser : IAddressParser
{
    private static readonly Dictionary<string, PartyRole> PartyElements = new(StringComparer.Ordinal)
    {
        ["Dbtr"] = PartyRole.Debtor,
        ["Cdtr"] = PartyRole.Creditor,
        ["UltmtDbtr"] = PartyRole.UltimateDebtor,
        ["UltmtCdtr"] = PartyRole.UltimateCreditor
    };

    public IngestionFormat Format => IngestionFormat.Iso20022Xml;

    public string Version => "iso20022-1.0";

    public ParseOutcome Parse(Stream payload, ParserOptions options)
    {
        var document = Load(payload);

        var messageId = document.Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "MsgId")?.Value.Trim();

        var addresses = new List<ParsedAddress>();
        var failures = new List<ParseFailure>();
        var excluded = 0;
        var sequence = 0;

        foreach (var partyElement in document.Descendants()
                     .Where(element => PartyElements.ContainsKey(element.Name.LocalName)))
        {
            sequence++;
            if (sequence > options.MaxRecords)
            {
                throw new ParserException($"The message exceeds the configured maximum of {options.MaxRecords} parties.");
            }

            var role = PartyElements[partyElement.Name.LocalName];
            var postalAddress = Child(partyElement, "PstlAdr");

            if (postalAddress is null)
            {
                // A party with no postal address at all is still an assessable finding (FR-VAL-002: Absent).
                addresses.Add(new ParsedAddress(
                    messageId,
                    EndToEndId(partyElement),
                    role,
                    Child(partyElement, "Nm")?.Value.Trim(),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null));
                continue;
            }

            var addressLines = postalAddress.Elements()
                .Where(element => element.Name.LocalName == "AdrLine")
                .Select(element => element.Value.Trim())
                .Where(value => value.Length > 0)
                .ToList();

            addresses.Add(new ParsedAddress(
                messageId,
                EndToEndId(partyElement),
                role,
                Child(partyElement, "Nm")?.Value.Trim(),
                Child(postalAddress, "Ctry")?.Value.Trim(),
                Child(postalAddress, "TwnNm")?.Value.Trim(),
                Child(postalAddress, "PstCd")?.Value.Trim(),
                Child(postalAddress, "StrtNm")?.Value.Trim(),
                Child(postalAddress, "BldgNb")?.Value.Trim(),
                addressLines.Count == 0 ? null : string.Join('\n', addressLines)));
        }

        if (sequence == 0)
        {
            throw new ParserException("No debtor or creditor parties were found in the message.");
        }

        return new ParseOutcome(addresses, failures, sequence, excluded);
    }

    private static XDocument Load(Stream payload)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true
        };

        try
        {
            using var reader = XmlReader.Create(payload, settings);
            return XDocument.Load(reader);
        }
        catch (XmlException exception)
        {
            throw new ParserException($"The payload is not well-formed XML: {exception.Message}");
        }
    }

    private static XElement? Child(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(element => element.Name.LocalName == localName);

    /// <summary>Walks up to the transaction the party belongs to and reads its end-to-end identifier.</summary>
    private static string? EndToEndId(XElement partyElement) =>
        partyElement.Ancestors()
            .Select(ancestor => ancestor.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "EndToEndId"))
            .FirstOrDefault(element => element is not null)
            ?.Value.Trim();
}
