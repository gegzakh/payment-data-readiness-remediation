using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using PDR.BuildingBlocks.Core.Paging;
using PDR.Remediation.Application.Cases;
using PDR.Remediation.Application.Cases.Commands;
using PDR.Remediation.Application.Upstream;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.IntegrationTests;

/// <summary>
/// Drives the case lifecycle over HTTP against a real database: generation and folding, maker edits,
/// evidence, submission, checker decisions and the funnel.
/// </summary>
public sealed class RemediationApiTests(RemediationApiFactory factory) : IClassFixture<RemediationApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly HttpClient _maker = factory.CreateClientAs("maker");
    private readonly HttpClient _checker = factory.CreateClientAs("checker");

    [Fact]
    public async Task Generation_opens_one_case_per_address_and_folds_repeat_payments()
    {
        var runId = Seed(
            "CBS",
            Assessment("CBS", town: null, messageId: "MSG-1"),
            Assessment("CBS", town: null, messageId: "MSG-2"),
            Assessment("CBS", town: "Berlin", partyName: "Beta AG", messageId: "MSG-3"));

        var generation = await GenerateAsync(runId);

        generation.CasesCreated.Should().Be(2);
        generation.OccurrencesFolded.Should().Be(3);

        var queue = await ReadAsync<PagedResult<CaseListItemDto>>(
            await _maker.GetAsync("/api/v1/remediation/cases?sourceCode=CBS", Token));

        var folded = queue.Items.Single(item => item.PartyName == "Acme GmbH");
        folded.Occurrences.Should().Be(2);
        folded.FutureExposure.Should().Be(2);
        folded.Status.Should().Be(CaseStatus.InProgress);
        folded.Queue.Should().Be("data-quality");
        folded.DueDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Regenerating_the_same_run_updates_the_existing_cases_instead_of_duplicating_them()
    {
        var runId = Seed("DUP", Assessment("DUP", town: null));

        var first = await GenerateAsync(runId);
        var second = await GenerateAsync(runId);

        first.CasesCreated.Should().Be(1);
        second.CasesCreated.Should().Be(0);
        second.CasesUpdated.Should().Be(1);
    }

    [Fact]
    public async Task A_generated_case_arrives_with_a_deterministic_proposal_and_its_original_values()
    {
        var runId = Seed(
            "PROP",
            Assessment("PROP", town: null, street: null, postCode: null, addressLines: "Hauptstrasse 12|10115 Berlin|Germany"));
        await GenerateAsync(runId);

        var detail = await FirstCaseAsync("PROP");

        detail.Original.AddressLines.Should().Be("Hauptstrasse 12|10115 Berlin|Germany");
        detail.Proposal.Should().NotBeNull();
        detail.Proposal!.Method.Should().Be(ProposalMethod.DeterministicParse);
        detail.Proposal.RequiresHumanVerification.Should().BeFalse();
        detail.Proposal.TownName.Should().Be("Berlin");
        detail.Proposal.PostCode.Should().Be("10115");
        detail.Proposal.OverallConfidence.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task A_full_maker_checker_round_trip_is_recorded_on_the_case()
    {
        var runId = Seed("FLOW", Assessment("FLOW", town: null));
        await GenerateAsync(runId);
        var caseId = (await FirstCaseAsync("FLOW")).Id;

        var edited = await ReadAsync<CaseDetailDto>(await _maker.PutAsJsonAsync(
            $"/api/v1/remediation/cases/{caseId}/proposal",
            new { country = "DE", townName = "Berlin", postCode = "10115", streetName = "Hauptstrasse", buildingNumber = "12", notes = "Confirmed with the customer" },
            Token));

        edited.Proposal!.Method.Should().Be(ProposalMethod.ManualEdit);
        edited.Status.Should().Be(CaseStatus.InProgress);

        await _maker.PostAsJsonAsync(
            $"/api/v1/remediation/cases/{caseId}/evidence",
            new { kind = "CustomerConfirmation", reference = "DOC-42", description = "Signed address confirmation" },
            Token);

        var submitted = await ReadAsync<CaseDetailDto>(
            await _maker.PostAsync($"/api/v1/remediation/cases/{caseId}/submit", null, Token));

        submitted.Status.Should().Be(CaseStatus.PendingApproval);
        submitted.SubmittedBy.Should().Be("maker");
        submitted.Evidence.Should().ContainSingle();

        var approved = await ReadAsync<CaseDetailDto>(await _checker.PostAsJsonAsync(
            $"/api/v1/remediation/cases/{caseId}/decision",
            new { decision = nameof(DecisionType.Approve), rationale = "Matches the register" },
            Token));

        approved.Status.Should().Be(CaseStatus.Approved);
        approved.DecidedBy.Should().Be("checker");
        approved.History.Select(entry => entry.Action)
            .Should().ContainInOrder("Opened", "Proposed", "Submitted", "Approve");
    }

    [Fact]
    public async Task The_maker_cannot_approve_their_own_case()
    {
        var runId = Seed("SOD", Assessment("SOD", town: null));
        await GenerateAsync(runId);
        var caseId = (await FirstCaseAsync("SOD")).Id;

        await _maker.PostAsync($"/api/v1/remediation/cases/{caseId}/submit", null, Token);

        var response = await _maker.PostAsJsonAsync(
            $"/api/v1/remediation/cases/{caseId}/decision",
            new { decision = nameof(DecisionType.Approve), rationale = "Looks fine to me" },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadAsync<CaseDetailDto>(await _maker.GetAsync($"/api/v1/remediation/cases/{caseId}", Token)))
            .Status.Should().Be(CaseStatus.PendingApproval);
    }

    [Fact]
    public async Task An_exception_is_time_bound_and_never_counts_as_remediated()
    {
        var runId = Seed("EXC", Assessment("EXC", town: null));
        await GenerateAsync(runId);
        var caseId = (await FirstCaseAsync("EXC")).Id;
        await _maker.PostAsync($"/api/v1/remediation/cases/{caseId}/submit", null, Token);

        var missingExpiry = await _checker.PostAsJsonAsync(
            $"/api/v1/remediation/cases/{caseId}/decision",
            new { decision = nameof(DecisionType.GrantException), rationale = "Customer is unreachable" },
            Token);
        missingExpiry.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var granted = await ReadAsync<CaseDetailDto>(await _checker.PostAsJsonAsync(
            $"/api/v1/remediation/cases/{caseId}/decision",
            new
            {
                decision = nameof(DecisionType.GrantException),
                rationale = "Customer is unreachable",
                exceptionExpiresOn = "2020-01-01"
            },
            Token));

        granted.Status.Should().Be(CaseStatus.ExceptionGranted);
        granted.IsExceptionExpired.Should().BeTrue();
        granted.RemediatedAtUtc.Should().BeNull();

        var funnel = await ReadAsync<RemediationFunnelDto>(
            await _maker.GetAsync("/api/v1/remediation/funnel", Token));

        funnel.ExceptionsGranted.Should().BeGreaterThan(0);
        funnel.ExpiredExceptions.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task A_returned_case_goes_back_to_the_maker_with_the_reason()
    {
        var runId = Seed("RET", Assessment("RET", town: null));
        await GenerateAsync(runId);
        var caseId = (await FirstCaseAsync("RET")).Id;
        await _maker.PostAsync($"/api/v1/remediation/cases/{caseId}/submit", null, Token);

        var returned = await ReadAsync<CaseDetailDto>(await _checker.PostAsJsonAsync(
            $"/api/v1/remediation/cases/{caseId}/decision",
            new { decision = nameof(DecisionType.Return), rationale = "Town does not match the postcode" },
            Token));

        returned.Status.Should().Be(CaseStatus.Returned);
        returned.DecisionRationale.Should().Be("Town does not match the postcode");
    }

    [Fact]
    public async Task A_bulk_approval_previews_its_blockers_and_skips_the_cases_the_caller_submitted()
    {
        var runId = Seed(
            "BULK",
            Assessment("BULK", town: null, partyName: "Bulk One"),
            Assessment("BULK", town: null, partyName: "Bulk Two"));
        await GenerateAsync(runId);

        var cases = await ReadAsync<PagedResult<CaseListItemDto>>(
            await _maker.GetAsync("/api/v1/remediation/cases?sourceCode=BULK", Token));

        foreach (var item in cases.Items)
        {
            await _maker.PostAsync($"/api/v1/remediation/cases/{item.Id}/submit", null, Token);
        }

        var selection = new { sourceCode = "BULK", status = nameof(CaseStatus.PendingApproval), minimumConfidence = 0m };

        var makerPreview = await ReadAsync<BulkPreviewDto>(await _maker.PostAsJsonAsync(
            "/api/v1/remediation/bulk/preview",
            new { action = BulkActions.Approve, selection },
            Token));

        makerPreview.EligibleCases.Should().Be(0);
        makerPreview.BlockedReasons.Should().Contain(reason => reason.StartsWith("the caller submitted it"));

        var checkerPreview = await ReadAsync<BulkPreviewDto>(await _checker.PostAsJsonAsync(
            "/api/v1/remediation/bulk/preview",
            new { action = BulkActions.Approve, selection },
            Token));

        checkerPreview.EligibleCases.Should().Be(2);
        checkerPreview.RollbackSupported.Should().BeTrue();
        checkerPreview.Sample.Should().HaveCount(2);

        var applied = await ReadAsync<BulkResultDto>(await _checker.PostAsJsonAsync(
            "/api/v1/remediation/bulk/apply",
            new { action = BulkActions.Approve, selection, rationale = "Deterministic parse, high confidence" },
            Token));

        applied.Applied.Should().Be(2);
        applied.Failures.Should().BeEmpty();
    }

    [Fact]
    public async Task A_bulk_approval_below_the_confidence_floor_is_blocked()
    {
        var runId = Seed("FLOOR", Assessment("FLOOR", town: null, addressLines: "PO Box 44"));
        await GenerateAsync(runId);
        var caseId = (await FirstCaseAsync("FLOOR")).Id;
        await _maker.PostAsync($"/api/v1/remediation/cases/{caseId}/submit", null, Token);

        var preview = await ReadAsync<BulkPreviewDto>(await _checker.PostAsJsonAsync(
            "/api/v1/remediation/bulk/preview",
            new { action = BulkActions.Approve, selection = new { sourceCode = "FLOOR", minimumConfidence = 90m } },
            Token));

        preview.EligibleCases.Should().Be(0);
        preview.BlockedReasons.Should().Contain(reason => reason.StartsWith("confidence below"));
    }

    [Fact]
    public async Task An_unknown_bulk_action_is_rejected_as_a_validation_error()
    {
        var response = await _checker.PostAsJsonAsync(
            "/api/v1/remediation/bulk/apply",
            new { action = "delete-everything", selection = new { sourceCode = "CBS" } },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Generating_from_an_unknown_run_reports_not_found()
    {
        var response = await _maker.PostAsJsonAsync(
            "/api/v1/remediation/cases/generate",
            new { runId = Guid.NewGuid() },
            Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_queue_pages_and_the_page_size_is_capped()
    {
        var response = await _maker.GetAsync("/api/v1/remediation/cases?page=1&pageSize=5000", Token);
        var page = await ReadAsync<PagedResult<CaseListItemDto>>(response);

        page.PageSize.Should().Be(RemediationDefaults.MaxPageSize);
        page.Page.Should().Be(1);
    }

    private async Task<CaseGenerationDto> GenerateAsync(Guid runId) =>
        await ReadAsync<CaseGenerationDto>(await _maker.PostAsJsonAsync(
            "/api/v1/remediation/cases/generate",
            new { runId },
            Token));

    private async Task<CaseDetailDto> FirstCaseAsync(string sourceCode)
    {
        var queue = await ReadAsync<PagedResult<CaseListItemDto>>(
            await _maker.GetAsync($"/api/v1/remediation/cases?sourceCode={sourceCode}", Token));

        return await ReadAsync<CaseDetailDto>(
            await _maker.GetAsync($"/api/v1/remediation/cases/{queue.Items[0].Id}", Token));
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "the request failed: {0}",
            await response.Content.ReadAsStringAsync(Token));

        return (await response.Content.ReadFromJsonAsync<T>(Json, Token))!;
    }

    private Guid Seed(string sourceCode, params AssessedAddress[] assessments)
    {
        var runId = Guid.NewGuid();
        factory.Validation.Add(
            new ValidationRunSummary(runId, Guid.NewGuid(), sourceCode, "SEPA", "Completed", DateOnly.FromDateTime(DateTime.UtcNow), assessments.Length, assessments.Length),
            assessments);

        return runId;
    }

    private static AssessedAddress Assessment(
        string sourceCode,
        string? country = "DE",
        string? town = "Berlin",
        string? postCode = "10115",
        string? street = "Hauptstrasse",
        string? addressLines = null,
        string partyName = "Acme GmbH",
        string messageId = "MSG-1") =>
        new(
            Guid.NewGuid(),
            sourceCode,
            "SEPA",
            messageId,
            $"E2E-{messageId}",
            PartyRole.Creditor,
            partyName,
            "Hybrid",
            "Warning",
            "Rejected",
            country,
            town,
            postCode,
            street,
            street is null ? null : "12",
            addressLines,
            $"batch/{sourceCode}/{messageId}",
            [new AssessedIssue("Future", "ADDR-STRUCT-001", "TownName", "Error", "Structured town is required")]);
}
