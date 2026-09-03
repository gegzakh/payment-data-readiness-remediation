using AwesomeAssertions;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.UnitTests.Domain;

public sealed class IngestionBatchTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private static IngestionBatch Receive() => IngestionBatch.Receive(
        "hub-eu",
        "feed.csv",
        IngestionFormat.Csv,
        IngestionChannel.Upload,
        128,
        new string('a', 64),
        "key-1",
        "csv-1.0",
        "pdr-admin",
        isReprocess: false,
        Now);

    [Fact]
    public void A_received_batch_normalises_its_source_code()
    {
        Receive().SourceCode.Should().Be("HUB-EU");
    }

    [Fact]
    public void A_quarantined_batch_cannot_be_parsed()
    {
        var batch = Receive();
        batch.Quarantine("File type not accepted.", Now);

        batch.Status.Should().Be(BatchStatus.Quarantined);
        batch.StartParsing(Now).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Completing_parsing_records_the_counts_and_reconciles()
    {
        var batch = Receive();
        batch.StartParsing(Now);
        batch.CompleteParsing(10, 7, 2, 1, 1, Now);

        batch.Status.Should().Be(BatchStatus.Parsed);
        batch.Checkpoint.Should().Be(10);
        batch.CountsReconcile().Should().BeTrue();
    }

    [Fact]
    public void Counts_that_do_not_add_up_to_the_input_fail_reconciliation()
    {
        var batch = Receive();
        batch.StartParsing(Now);
        batch.CompleteParsing(10, 5, 1, 0, 1, Now);

        batch.CountsReconcile().Should().BeFalse();
    }

    [Fact]
    public void Only_a_failed_batch_may_be_retried()
    {
        var batch = Receive();
        batch.StartParsing(Now);

        batch.PrepareRetry(Now).IsFailure.Should().BeTrue();

        batch.Fail("Header missing.", Now);
        batch.PrepareRetry(Now).IsSuccess.Should().BeTrue();
        batch.RetryCount.Should().Be(1);
        batch.ErrorSummary.Should().BeNull();
        batch.Status.Should().Be(BatchStatus.Parsing);
    }

    [Fact]
    public void A_parsed_batch_cannot_be_cancelled()
    {
        var batch = Receive();
        batch.StartParsing(Now);
        batch.CompleteParsing(1, 1, 0, 0, 0, Now);

        batch.Cancel(Now).Error.Code.Should().Be("BATCH.ALREADY_PARSED");
    }

    [Fact]
    public void A_checkpoint_never_moves_backwards()
    {
        var batch = Receive();
        batch.RecordCheckpoint(500);
        batch.RecordCheckpoint(200);

        batch.Checkpoint.Should().Be(500);
    }
}

public sealed class PartyAddressRecordTests
{
    private static PartyAddressRecord Create(string? town, string? country = "DE") =>
        PartyAddressRecord.Create(
            Guid.NewGuid(),
            1,
            "MSG",
            "E2E",
            PartyRole.Creditor,
            "Acme",
            country,
            town,
            "10115",
            "High Street",
            "10",
            null,
            "sepa");

    [Fact]
    public void The_content_hash_ignores_case_and_spacing()
    {
        Create("  berlin ").ContentHash.Should().Be(Create("Berlin").ContentHash);
    }

    [Fact]
    public void Different_addresses_hash_differently()
    {
        Create("Berlin").ContentHash.Should().NotBe(Create("Hamburg").ContentHash);
    }

    [Fact]
    public void Country_and_scheme_are_stored_upper_case()
    {
        var record = Create("Berlin", "de");

        record.Country.Should().Be("DE");
        record.SchemeCode.Should().Be("SEPA");
    }
}
