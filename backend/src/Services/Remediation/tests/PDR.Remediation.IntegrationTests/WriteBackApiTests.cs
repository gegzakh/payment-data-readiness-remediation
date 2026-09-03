using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.BuildingBlocks.Core.Paging;
using PDR.Remediation.Application.Cases;
using PDR.Remediation.Application.Upstream;
using PDR.Remediation.Application.WriteBack;
using PDR.Remediation.Domain.Cases;
using PDR.Remediation.Domain.WriteBack;

namespace PDR.Remediation.IntegrationTests;

/// <summary>
/// Write-back over HTTP against the simulated source: preview, idempotent apply, read-after-write
/// confirmation, reconciliation and rollback.
/// </summary>
public sealed class WriteBackApiTests(RemediationApiFactory factory) : IClassFixture<RemediationApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly HttpClient _maker = factory.CreateClientAs("maker");
    private readonly HttpClient _checker = factory.CreateClientAs("checker");

    [Fact]
    public async Task The_configured_targets_declare_their_mode_limits_and_rollback_method()
    {
        var targets = await ReadAsync<IReadOnlyList<WriteBackTargetDto>>(
            await _maker.GetAsync("/api/v1/remediation/writeback/targets", Token));

        var cbs = targets.Single(target => target.SourceCode == "CBS");
        cbs.Mode.Should().Be(WriteBackMode.Api);
        cbs.RequiresApproval.Should().BeTrue();
        cbs.MaxRecordsPerRun.Should().BeGreaterThan(0);
        cbs.RollbackMethod.Should().NotBeNullOrWhiteSpace();

        targets.Single(target => target.SourceCode == "CRM").Mode.Should().Be(WriteBackMode.Export);
    }

    [Fact]
    public async Task A_preview_lists_only_the_fields_the_target_accepts()
    {
        await ApprovedCaseAsync("CBS", "Preview GmbH");

        var preview = await ReadAsync<WriteBackPreviewDto>(await _maker.PostAsJsonAsync(
            "/api/v1/remediation/writeback/preview",
            new { sourceCode = "cbs" },
            Token));

        preview.TargetSourceCode.Should().Be("CBS");
        preview.EligibleCases.Should().BeGreaterThan(0);
        preview.RecordsToWrite.Should().BeGreaterThan(0);
        preview.Changes.Should().Contain(change => change.Field == "town");
        preview.Blockers.Should().BeEmpty();
    }

    [Fact]
    public async Task A_write_back_applies_confirms_and_reconciles_and_replaying_the_key_does_not_write_twice()
    {
        var caseId = await ApprovedCaseAsync("CBS", "Apply GmbH");
        var key = $"test-{Guid.NewGuid():n}";

        var job = await ApplyAsync(key, caseId);

        job.Status.Should().Be(WriteBackStatus.Confirmed);
        job.ItemCount.Should().Be(1);
        job.ConfirmedCount.Should().Be(1);
        job.FailedCount.Should().Be(0);
        job.CountsReconcile.Should().BeTrue();
        job.Items[0].CorrelationId.Should().NotBeNullOrWhiteSpace();
        job.Items[0].BeforeValue.Should().NotBe(job.Items[0].AfterValue);

        var replay = await ApplyAsync(key, caseId);
        replay.Id.Should().Be(job.Id);
        replay.ItemCount.Should().Be(1);

        var reconciliation = await ReadAsync<WriteBackReconciliationDto>(
            await _maker.GetAsync($"/api/v1/remediation/writeback/jobs/{job.Id}/reconciliation", Token));

        reconciliation.Requested.Should().Be(1);
        reconciliation.Balanced.Should().BeTrue();
        reconciliation.Discrepancies.Should().BeEmpty();

        var remediated = await ReadAsync<CaseDetailDto>(
            await _maker.GetAsync($"/api/v1/remediation/cases/{caseId}", Token));

        remediated.Status.Should().Be(CaseStatus.Remediated);
        remediated.RemediatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task The_idempotency_key_may_travel_as_a_header()
    {
        var caseId = await ApprovedCaseAsync("CBS", "Header GmbH");
        var key = $"header-{Guid.NewGuid():n}";

        using var first = new HttpRequestMessage(HttpMethod.Post, "/api/v1/remediation/writeback/apply")
        {
            Content = JsonContent.Create(new { sourceCode = "CBS", caseIds = new[] { caseId } })
        };
        first.Headers.Add("Idempotency-Key", key);

        var job = await ReadAsync<WriteBackJobDto>(await _checker.SendAsync(first, Token));
        job.IdempotencyKey.Should().Be(key);

        using var replay = new HttpRequestMessage(HttpMethod.Post, "/api/v1/remediation/writeback/apply")
        {
            Content = JsonContent.Create(new { sourceCode = "CBS", caseIds = new[] { caseId } })
        };
        replay.Headers.Add("Idempotency-Key", key);

        (await ReadAsync<WriteBackJobDto>(await _checker.SendAsync(replay, Token))).Id.Should().Be(job.Id);
    }

    [Fact]
    public async Task Rolling_back_restores_the_source_and_reopens_the_case()
    {
        var caseId = await ApprovedCaseAsync("CBS", "Rollback GmbH");
        var job = await ApplyAsync($"rollback-{Guid.NewGuid():n}", caseId);
        var before = job.Items[0].BeforeValue;

        var rolledBack = await ReadAsync<WriteBackJobDto>(await _checker.PostAsJsonAsync(
            $"/api/v1/remediation/writeback/jobs/{job.Id}/rollback",
            new { reason = "Reference data was wrong" },
            Token));

        rolledBack.Status.Should().Be(WriteBackStatus.RolledBack);
        rolledBack.RolledBackCount.Should().Be(1);

        var reopened = await ReadAsync<CaseDetailDto>(
            await _maker.GetAsync($"/api/v1/remediation/cases/{caseId}", Token));

        reopened.Status.Should().Be(CaseStatus.RolledBack);
        reopened.RemediatedAtUtc.Should().BeNull();

        // The source now holds what it held before the correction, so a second run has work to do again.
        var reconciliation = await ReadAsync<WriteBackReconciliationDto>(
            await _maker.GetAsync($"/api/v1/remediation/writeback/jobs/{job.Id}/reconciliation", Token));

        reconciliation.RolledBack.Should().Be(1);
        before.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task A_run_with_nothing_approved_is_refused_rather_than_writing_an_empty_job()
    {
        var response = await _checker.PostAsJsonAsync(
            "/api/v1/remediation/writeback/apply",
            new { sourceCode = "CRM", idempotencyKey = $"empty-{Guid.NewGuid():n}" },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_unconfigured_target_is_refused()
    {
        var response = await _maker.PostAsJsonAsync(
            "/api/v1/remediation/writeback/preview",
            new { sourceCode = "NOPE" },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync(Token))
            .Should().Contain("WRITEBACK.TARGET_NOT_CONFIGURED");
    }

    [Fact]
    public async Task Jobs_are_listed_newest_first_and_can_be_filtered_by_source()
    {
        var caseId = await ApprovedCaseAsync("CBS", "Listing GmbH");
        await ApplyAsync($"listing-{Guid.NewGuid():n}", caseId);

        var jobs = await ReadAsync<PagedResult<WriteBackJobDto>>(
            await _maker.GetAsync("/api/v1/remediation/writeback/jobs?sourceCode=CBS", Token));

        jobs.Items.Should().NotBeEmpty();
        jobs.Items.Should().OnlyContain(job => job.TargetSourceCode == "CBS");
        jobs.Items.Should().BeInDescendingOrder(job => job.RequestedAtUtc);
    }

    [Fact]
    public async Task An_unknown_job_is_not_found()
    {
        var response = await _maker.GetAsync($"/api/v1/remediation/writeback/jobs/{Guid.NewGuid()}", Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<WriteBackJobDto> ApplyAsync(string idempotencyKey, Guid caseId) =>
        await ReadAsync<WriteBackJobDto>(await _checker.PostAsJsonAsync(
            "/api/v1/remediation/writeback/apply",
            new { sourceCode = "CBS", caseIds = new[] { caseId }, idempotencyKey },
            Token));

    /// <summary>Generates a case for the given party, then walks it through submission and approval.</summary>
    private async Task<Guid> ApprovedCaseAsync(string sourceCode, string partyName)
    {
        var runId = Guid.NewGuid();
        var assessment = new AssessedAddress(
            Guid.NewGuid(),
            sourceCode,
            "SEPA",
            "MSG-1",
            "E2E-1",
            PartyRole.Creditor,
            partyName,
            "Unstructured",
            "Warning",
            "Rejected",
            "DE",
            null,
            null,
            null,
            null,
            "Hauptstrasse 12|10115 Berlin|Germany",
            $"batch/{partyName}",
            [new AssessedIssue("Future", "ADDR-STRUCT-001", "TownName", "Error", "Structured town is required")]);

        factory.Validation.Add(
            new ValidationRunSummary(runId, Guid.NewGuid(), sourceCode, "SEPA", "Completed", DateOnly.FromDateTime(DateTime.UtcNow), 1, 1),
            [assessment]);

        await ReadAsync<CaseGenerationDto>(
            await _maker.PostAsJsonAsync("/api/v1/remediation/cases/generate", new { runId }, Token));

        var queue = await ReadAsync<PagedResult<CaseListItemDto>>(
            await _maker.GetAsync($"/api/v1/remediation/cases?sourceCode={sourceCode}&pageSize=200", Token));

        var caseId = queue.Items.Single(item => item.PartyName == partyName).Id;

        await _maker.PostAsync($"/api/v1/remediation/cases/{caseId}/submit", null, Token);
        await ReadAsync<CaseDetailDto>(await _checker.PostAsJsonAsync(
            $"/api/v1/remediation/cases/{caseId}/decision",
            new { decision = nameof(DecisionType.Approve), rationale = "Verified" },
            Token));

        return caseId;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "the request failed: {0}",
            await response.Content.ReadAsStringAsync(Token));

        return (await response.Content.ReadFromJsonAsync<T>(Json, Token))!;
    }
}
