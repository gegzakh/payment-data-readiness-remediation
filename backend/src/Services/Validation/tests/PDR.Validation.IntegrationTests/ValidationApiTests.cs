using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.BuildingBlocks.Core.Paging;
using PDR.Validation.Application.Assess;
using PDR.Validation.Application.Upstream;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.IntegrationTests;

public sealed class ValidationApiTests(ValidationApiFactory factory) : IClassFixture<ValidationApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static IngestedRecord Record(
        int sequence,
        string? country = "DE",
        string? townName = "Berlin",
        string? addressLines = null,
        bool isDuplicate = false) =>
        new(
            Guid.NewGuid(),
            Guid.Empty,
            sequence,
            $"MSG-{sequence}",
            $"E2E-{sequence}",
            PartyRole.Creditor,
            "Acme GmbH",
            country,
            townName,
            "10115",
            addressLines is null ? "Invalidenstrasse" : null,
            addressLines is null ? "12" : null,
            addressLines,
            "SEPA",
            isDuplicate);

    private Guid SeedBatch(string sourceCode, params IngestedRecord[] records)
    {
        var batchId = Guid.NewGuid();
        factory.Ingestion.Add(
            new IngestedBatch(batchId, sourceCode, "Parsed", records.Length),
            records.Select(record => record with { BatchId = batchId }));

        return batchId;
    }

    private async Task<ValidationRunDto> RunAsync(Guid batchId)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/validation/runs",
            new { batchId, asOf = new DateOnly(2026, 1, 15) },
            Json,
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ValidationRunDto>(Json, Token))!;
    }

    [Fact]
    public async Task A_run_scores_current_and_future_readiness_and_reconciles()
    {
        var batchId = SeedBatch(
            "CORE-EU",
            Record(1),
            Record(2, addressLines: "12 Invalidenstrasse Berlin"),
            Record(3, townName: null),
            Record(4, isDuplicate: true));

        var run = await RunAsync(batchId);

        run.Status.Should().Be(ValidationRunStatus.Completed);
        run.InputRecordCount.Should().Be(4);
        run.AssessedCount.Should().Be(3);
        run.ExcludedCount.Should().Be(1);
        run.CurrentRulesetVersion.Should().Be(1);
        run.FutureRulesetVersion.Should().Be(2);
        run.CurrentReadinessPercent.Should().Be(100m, "only the country rule applies today");
        run.FutureReadinessPercent.Should().BeApproximately(33.33m, 0.01m);
        run.PaymentsAtRisk.Should().Be(2);
        run.CountsReconcile.Should().BeTrue();
    }

    [Fact]
    public async Task Findings_explain_a_rejection_and_point_back_to_the_batch()
    {
        var batchId = SeedBatch("CORE-FIND", Record(1, townName: null));

        var run = await RunAsync(batchId);

        var assessments = await _client.GetFromJsonAsync<PagedResult<AddressAssessmentDto>>(
            $"/api/v1/validation/runs/{run.Id}/assessments?mode=Future&outcome=Rejected", Json, Token);

        var assessment = assessments!.Items.Should().ContainSingle().Subject;
        assessment.EvidencePointer.Should().Be($"batch:{batchId}#record:1");
        assessment.Issues.Should().Contain(issue => issue.RuleCode == "ADDR.TOWN.REQ" && issue.Mode == RuleMode.Future);
    }

    [Fact]
    public async Task Address_detail_is_masked_without_the_drill_down_permission()
    {
        var batchId = SeedBatch("CORE-MASK", Record(1));

        var run = await RunAsync(batchId);

        var assessments = await _client.GetFromJsonAsync<PagedResult<AddressAssessmentDto>>(
            $"/api/v1/validation/runs/{run.Id}/assessments", Json, Token);

        // Authentication is off in this factory, so the caller holds no permissions at all.
        assessments!.Items.Should().OnlyContain(item => item.StreetName!.Contains('*'));
        assessments.Items.Should().OnlyContain(item => item.Country == "DE");
    }

    [Fact]
    public async Task An_unparsed_batch_cannot_be_validated()
    {
        var batchId = Guid.NewGuid();
        factory.Ingestion.Add(new IngestedBatch(batchId, "CORE-RAW", "Received", 0), []);

        var response = await _client.PostAsJsonAsync("/api/v1/validation/runs", new { batchId }, Json, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_unknown_batch_is_reported_as_not_found()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/validation/runs",
            new { batchId = Guid.NewGuid() },
            Json,
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Profiles_and_the_readiness_summary_aggregate_completed_runs()
    {
        var batchId = SeedBatch("CORE-PROFILE", Record(1), Record(2, townName: null));
        var run = await RunAsync(batchId);

        var profile = await _client.GetFromJsonAsync<ProfileDto>(
            $"/api/v1/validation/profile?dimension=Source&runId={run.Id}", Json, Token);

        profile!.Rows.Should().ContainSingle(row => row.Key == "CORE-PROFILE");

        var issues = await _client.GetFromJsonAsync<ProfileDto>(
            $"/api/v1/validation/profile?dimension=Issue&runId={run.Id}", Json, Token);

        issues!.Rows.Should().Contain(row => row.Key.StartsWith("ADDR.", StringComparison.Ordinal));

        var summary = await _client.GetFromJsonAsync<ReadinessSummaryDto>(
            "/api/v1/validation/readiness", Json, Token);

        summary!.RunCount.Should().BeGreaterThan(0);
        summary.PaymentsAtRisk.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Profile_rows_count_the_assessed_population_and_score_issue_readiness()
    {
        var batchId = SeedBatch(
            "CORE-DENOM",
            Record(1),
            Record(2, townName: null),
            Record(3, isDuplicate: true));

        var run = await RunAsync(batchId);

        var profile = await _client.GetFromJsonAsync<ProfileDto>(
            $"/api/v1/validation/profile?dimension=Source&runId={run.Id}", Json, Token);

        var row = profile!.Rows.Should().ContainSingle(entry => entry.Key == "CORE-DENOM").Subject;
        row.RecordCount.Should().Be(run.AssessedCount, "excluded records are outside the readiness denominator");
        row.FutureRejectedCount.Should().Be(1);
        row.FutureReadinessPercent.Should().BeApproximately(50m, 0.01m);

        var issues = await _client.GetFromJsonAsync<ProfileDto>(
            $"/api/v1/validation/profile?dimension=Issue&runId={run.Id}", Json, Token);

        var issueRow = issues!.Rows.Should().ContainSingle(entry => entry.Key == "ADDR.TOWN.REQ").Subject;
        issueRow.RecordCount.Should().Be(1);
        issueRow.FutureRejectedCount.Should().Be(1);
        issueRow.FutureReadinessPercent.Should().Be(0m);
        issueRow.CurrentReadinessPercent.Should().Be(100m, "the town rule only bites after cutover");
    }

    [Fact]
    public async Task Runs_are_listed_newest_first_and_filterable_by_batch()
    {
        var batchId = SeedBatch("CORE-LIST", Record(1));
        var run = await RunAsync(batchId);

        var page = await _client.GetFromJsonAsync<PagedResult<ValidationRunDto>>(
            $"/api/v1/validation/runs?batchId={batchId}", Json, Token);

        page!.Items.Should().ContainSingle().Which.Id.Should().Be(run.Id);

        var detail = await _client.GetFromJsonAsync<ValidationRunDto>(
            $"/api/v1/validation/runs/{run.Id}", Json, Token);

        detail!.SourceCode.Should().Be("CORE-LIST");
    }

    [Fact]
    public async Task A_missing_future_rule_set_leaves_records_unable_to_assess()
    {
        factory.Rules.FutureRules = null;

        try
        {
            var batchId = SeedBatch("CORE-NORULES", Record(1));

            var run = await RunAsync(batchId);

            run.UnableToAssessCount.Should().Be(0, "the current rule set is still available");
            run.FutureRulesetVersion.Should().BeNull();

            var assessments = await _client.GetFromJsonAsync<PagedResult<AddressAssessmentDto>>(
                $"/api/v1/validation/runs/{run.Id}/assessments", Json, Token);

            assessments!.Items.Should().OnlyContain(item => item.FutureOutcome == RecordOutcome.UnableToAssess);
        }
        finally
        {
            factory.Rules.FutureRules =
            [
                new("ADDR.CTRY.REQ", "Country", RuleCheck.Required, IssueSeverity.Error, "Country is mandatory.", null),
                new("ADDR.TOWN.REQ", "TownName", RuleCheck.Required, IssueSeverity.Error, "Town name is mandatory.", null),
                new("ADDR.STRUCT", "AddressLine", RuleCheck.StructuredOnly, IssueSeverity.Error, "Structured address required.", null)
            ];
        }
    }

    [Fact]
    public async Task Page_size_is_configurable_at_runtime()
    {
        var response = await _client.GetAsync("/api/v1/settings", Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(Token);
        body.Should().Contain("validation.page-size");
    }
}
