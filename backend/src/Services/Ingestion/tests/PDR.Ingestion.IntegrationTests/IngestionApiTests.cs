using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.BuildingBlocks.Core.Paging;
using PDR.Ingestion.Application.Ingest;

namespace PDR.Ingestion.IntegrationTests;

public sealed class IngestionApiTests(IngestionApiFactory factory) : IClassFixture<IngestionApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private const string Csv =
        """
        PartyRole,PartyName,Country,Town,PostCode,Street,BuildingNumber
        Debtor,Acme GmbH,DE,Berlin,10115,Chausseestrasse,10
        Creditor,Beta SARL,FR,Paris,75001,Rue de Rivoli,12
        Creditor,Beta SARL,FR,Paris,75001,Rue de Rivoli,12
        """;

    private const string Pain001 =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:pain.001.001.09">
          <CstmrCdtTrfInitn>
            <GrpHdr><MsgId>MSG-INT-1</MsgId></GrpHdr>
            <PmtInf>
              <Dbtr><Nm>Debtor Ltd</Nm><PstlAdr><StrtNm>High Street</StrtNm><BldgNb>10</BldgNb><TwnNm>London</TwnNm><Ctry>GB</Ctry></PstlAdr></Dbtr>
              <CdtTrfTxInf>
                <PmtId><EndToEndId>E2E-INT-1</EndToEndId></PmtId>
                <Cdtr><Nm>Creditor SA</Nm><PstlAdr><AdrLine>12 Rue de Rivoli</AdrLine></PstlAdr></Cdtr>
              </CdtTrfTxInf>
            </PmtInf>
          </CstmrCdtTrfInitn>
        </Document>
        """;

    private async Task<HttpResponseMessage> UploadAsync(
        string fileName,
        string content,
        string format,
        string sourceCode = "HUB-EU",
        bool reprocess = false,
        string? idempotencyKey = null)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        form.Add(file, "file", fileName);

        var query = $"/api/v1/batches/upload?sourceCode={sourceCode}&format={format}&reprocess={reprocess}" +
                    (idempotencyKey is null ? string.Empty : $"&idempotencyKey={idempotencyKey}");

        return await _client.PostAsync(query, form, Token);
    }

    private static async Task<IngestionBatchDto> ReadBatchAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<IngestionBatchDto>(Json, Token))!;

    [Fact]
    public async Task A_delimited_upload_is_parsed_deduplicated_and_reconciled()
    {
        var response = await UploadAsync("feed-1.csv", Csv, "Csv", sourceCode: "CSV-HAPPY", idempotencyKey: "csv-happy-path");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var batch = await ReadBatchAsync(response);

        batch.Status.ToString().Should().Be("Parsed");
        batch.RecordCount.Should().Be(3);
        batch.ParsedCount.Should().Be(3);
        batch.DuplicateCount.Should().Be(1);
        batch.CountsReconcile.Should().BeTrue();
        batch.Checksum.Should().HaveLength(64);
        batch.ParserVersion.Should().Be("csv-1.0");

        var records = await _client.GetFromJsonAsync<PagedResult<PartyAddressRecordDto>>(
            $"/api/v1/batches/{batch.Id}/records", Json, Token);

        records!.TotalCount.Should().Be(3);
        records.Items.Should().Contain(record => record.IsDuplicate);
    }

    [Fact]
    public async Task Records_are_masked_for_callers_without_the_drill_down_permission()
    {
        var batch = await ReadBatchAsync(
            await UploadAsync("feed-mask.csv", Csv, "Csv", sourceCode: "CSV-MASK", idempotencyKey: "csv-mask"));

        var records = await _client.GetFromJsonAsync<PagedResult<PartyAddressRecordDto>>(
            $"/api/v1/batches/{batch.Id}/records", Json, Token);

        // Authentication is off in this factory, so the caller holds no permissions at all.
        records!.Items.Should().OnlyContain(record => record.PartyName!.Contains('*'));
        records.Items.Should().OnlyContain(record => record.Country == "DE" || record.Country == "FR");
    }

    [Fact]
    public async Task An_iso20022_message_yields_structured_and_unstructured_parties()
    {
        var batch = await ReadBatchAsync(
            await UploadAsync("pain001.xml", Pain001, "Iso20022Xml", sourceCode: "XML-HAPPY", idempotencyKey: "xml-happy"));

        batch.Status.ToString().Should().Be("Parsed");
        batch.ParsedCount.Should().Be(2);
        batch.ParserVersion.Should().Be("iso20022-1.0");

        var records = await _client.GetFromJsonAsync<PagedResult<PartyAddressRecordDto>>(
            $"/api/v1/batches/{batch.Id}/records", Json, Token);

        records!.Items.Should().Contain(record => record.Country == "GB");
        records.Items.Should().Contain(record => record.AddressLines != null);
    }

    [Fact]
    public async Task An_unsafe_payload_is_quarantined_with_a_non_sensitive_reason()
    {
        var batch = await ReadBatchAsync(
            await UploadAsync("payload.bin", "PartyRole,Country", "Csv", sourceCode: "UNSAFE-SRC", idempotencyKey: "unsafe-type"));

        batch.Status.ToString().Should().Be("Quarantined");
        batch.QuarantineReason.Should().Contain("not accepted");
        batch.ParsedCount.Should().Be(0);
    }

    [Fact]
    public async Task An_unparsable_payload_fails_the_batch_and_can_be_retried()
    {
        var batch = await ReadBatchAsync(
            await UploadAsync(
                "broken.csv", "Country,Town\nDE,Berlin", "Csv", sourceCode: "BROKEN-SRC", idempotencyKey: "broken-header"));

        batch.Status.ToString().Should().Be("Failed");
        batch.ErrorSummary.Should().Contain("partyrole");

        var retry = await _client.PostAsync($"/api/v1/batches/{batch.Id}/retry", null, Token);
        retry.StatusCode.Should().Be(HttpStatusCode.OK);

        var retried = await ReadBatchAsync(retry);
        retried.RetryCount.Should().Be(1);
        retried.Status.ToString().Should().Be("Failed");
    }

    [Fact]
    public async Task Re_uploading_the_same_payload_is_refused_unless_reprocess_is_requested()
    {
        await UploadAsync("dup.csv", Csv, "Csv", sourceCode: "DUP-SRC", idempotencyKey: "dup-first");

        var duplicate = await UploadAsync("dup.csv", Csv, "Csv", sourceCode: "DUP-SRC", idempotencyKey: "dup-second");
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var reprocess = await UploadAsync(
            "dup.csv", Csv, "Csv", sourceCode: "DUP-SRC", reprocess: true, idempotencyKey: "dup-third");
        reprocess.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ReadBatchAsync(reprocess)).IsReprocess.Should().BeTrue();
    }

    [Fact]
    public async Task Replaying_an_idempotency_key_returns_the_original_batch()
    {
        var first = await ReadBatchAsync(
            await UploadAsync("idem.csv", Csv, "Csv", sourceCode: "IDEM-SRC", idempotencyKey: "idem-key"));
        var replay = await ReadBatchAsync(
            await UploadAsync("idem.csv", Csv, "Csv", sourceCode: "IDEM-SRC", idempotencyKey: "idem-key"));

        replay.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task The_overview_and_listing_expose_the_ingested_batches()
    {
        await UploadAsync("overview.csv", Csv, "Csv", sourceCode: "OVW-SRC", idempotencyKey: "overview-1");

        var overview = await _client.GetFromJsonAsync<IngestionOverviewDto>("/api/v1/batches/overview", Json, Token);
        overview!.TotalBatches.Should().BeGreaterThan(0);
        overview.ParsedBatches.Should().BeGreaterThan(0);

        var listing = await _client.GetFromJsonAsync<PagedResult<IngestionBatchDto>>(
            "/api/v1/batches?sourceCode=OVW-SRC", Json, Token);

        listing!.Items.Should().ContainSingle().Which.SourceCode.Should().Be("OVW-SRC");
    }

    [Fact]
    public async Task An_unknown_batch_is_reported_as_not_found()
    {
        var response = await _client.GetAsync($"/api/v1/batches/{Guid.NewGuid()}", Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ingestion_tunables_are_readable_at_runtime()
    {
        var response = await _client.GetAsync("/api/v1/settings", Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(Token);
        body.Should().Contain(IngestionSettingKeys.MaxFileBytes);
    }
}
