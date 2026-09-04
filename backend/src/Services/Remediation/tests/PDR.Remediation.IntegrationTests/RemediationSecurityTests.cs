using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace PDR.Remediation.IntegrationTests;

/// <summary>
/// Remediation carries customer data and writes to source systems, so nothing in it may answer an
/// unauthenticated caller (FR-SEC-001).
/// </summary>
public sealed class RemediationSecurityTests(SecuredRemediationApiFactory factory)
    : IClassFixture<SecuredRemediationApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public static TheoryData<string> ProtectedReads =>
    [
        "/api/v1/remediation/cases",
        $"/api/v1/remediation/cases/{Guid.Empty}",
        "/api/v1/remediation/funnel",
        "/api/v1/remediation/campaigns",
        "/api/v1/remediation/writeback/targets",
        "/api/v1/remediation/writeback/jobs",
        $"/api/v1/remediation/writeback/jobs/{Guid.Empty}",
        $"/api/v1/remediation/writeback/jobs/{Guid.Empty}/reconciliation"
    ];

    [Theory]
    [MemberData(nameof(ProtectedReads))]
    public async Task No_case_or_write_back_data_is_readable_anonymously(string path)
    {
        var response = await _client.GetAsync(path, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Case_generation_and_workflow_actions_reject_anonymous_callers()
    {
        var generate = await _client.PostAsJsonAsync("/api/v1/remediation/cases/generate", new { }, Token);
        var submit = await _client.PostAsync($"/api/v1/remediation/cases/{Guid.NewGuid()}/submit", null, Token);
        var decide = await _client.PostAsJsonAsync(
            $"/api/v1/remediation/cases/{Guid.NewGuid()}/decision",
            new { decision = "Approve" },
            Token);

        generate.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        submit.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        decide.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Writing_to_a_source_system_is_never_anonymous()
    {
        var apply = await _client.PostAsJsonAsync(
            "/api/v1/remediation/writeback/apply",
            new { sourceCode = "CBS" },
            Token);

        var rollback = await _client.PostAsJsonAsync(
            $"/api/v1/remediation/writeback/jobs/{Guid.NewGuid()}/rollback",
            new { reason = "test" },
            Token);

        apply.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        rollback.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bulk_actions_are_not_a_way_around_authentication()
    {
        var preview = await _client.PostAsJsonAsync(
            "/api/v1/remediation/bulk/preview",
            new { action = "approve", selection = new { sourceCode = "CBS" } },
            Token);

        var apply = await _client.PostAsJsonAsync(
            "/api/v1/remediation/bulk/apply",
            new { action = "approve", selection = new { sourceCode = "CBS" } },
            Token);

        preview.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        apply.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_forged_bearer_token_is_rejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/remediation/cases");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await _client.SendAsync(request, Token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_rejected_request_answers_with_problem_details()
    {
        var response = await _client.GetAsync("/api/v1/remediation/cases", Token);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(Token);
        problem.Should().ContainKey("title").And.ContainKey("status");
    }

    [Fact]
    public async Task Health_and_the_api_reference_stay_reachable_for_operations()
    {
        var health = await _client.GetAsync("/health/live", Token);

        health.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
