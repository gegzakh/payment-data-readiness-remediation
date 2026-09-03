using System.Text;
using AwesomeAssertions;
using PDR.Ingestion.Application.Ingest;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.UnitTests.Ingest;

public sealed class FileSafetyInspectorTests
{
    private static readonly FileSafetyOptions Options = new(1024, [".xml", ".csv"], 100, ",");

    private static FileInspection Inspect(string fileName, IngestionFormat format, string content) =>
        FileSafetyInspector.Inspect(fileName, format, Encoding.UTF8.GetBytes(content), Options);

    private static FileInspection Inspect(string fileName, IngestionFormat format, byte[] content) =>
        FileSafetyInspector.Inspect(fileName, format, content, Options);

    [Fact]
    public void A_valid_payload_passes_and_is_checksummed()
    {
        var inspection = Inspect("feed.csv", IngestionFormat.Csv, "PartyRole,Country\nDebtor,DE");

        inspection.IsSafe.Should().BeTrue();
        inspection.RejectionReason.Should().BeNull();
        inspection.Checksum.Should().HaveLength(64);
    }

    [Fact]
    public void The_checksum_is_stable_for_identical_content()
    {
        var first = Inspect("a.csv", IngestionFormat.Csv, "PartyRole\nDebtor");
        var second = Inspect("b.csv", IngestionFormat.Csv, "PartyRole\nDebtor");

        second.Checksum.Should().Be(first.Checksum);
    }

    [Fact]
    public void An_empty_payload_is_rejected()
    {
        var inspection = Inspect("feed.csv", IngestionFormat.Csv, Array.Empty<byte>());

        inspection.IsSafe.Should().BeFalse();
        inspection.RejectionReason.Should().Contain("empty");
    }

    [Fact]
    public void An_oversized_payload_is_rejected()
    {
        var inspection = Inspect("feed.csv", IngestionFormat.Csv, new string('a', 2048));

        inspection.RejectionReason.Should().Contain("maximum accepted size");
    }

    [Fact]
    public void An_unaccepted_file_type_is_rejected()
    {
        var inspection = Inspect("payload.exe", IngestionFormat.Csv, "PartyRole");

        inspection.RejectionReason.Should().Contain("not accepted");
    }

    [Fact]
    public void Invalid_utf8_is_rejected()
    {
        var inspection = Inspect("feed.csv", IngestionFormat.Csv, new byte[] { 0xC3, 0x28 });

        inspection.RejectionReason.Should().Contain("UTF-8");
    }

    [Fact]
    public void A_malware_signature_is_rejected()
    {
        var inspection = Inspect("feed.csv", IngestionFormat.Csv, @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD");

        inspection.RejectionReason.Should().Contain("malware");
    }

    [Fact]
    public void An_entity_declaration_is_rejected_before_parsing()
    {
        var inspection = Inspect(
            "bomb.xml",
            IngestionFormat.Iso20022Xml,
            "<?xml version=\"1.0\"?><!DOCTYPE x [<!ENTITY a \"b\">]><Document/>");

        inspection.RejectionReason.Should().Contain("entity declarations");
    }

    [Fact]
    public void A_non_xml_payload_declared_as_iso20022_is_rejected()
    {
        var inspection = Inspect("feed.xml", IngestionFormat.Iso20022Xml, "PartyRole,Country");

        inspection.RejectionReason.Should().Contain("XML document");
    }
}
