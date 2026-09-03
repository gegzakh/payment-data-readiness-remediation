using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using PDR.Remediation.Application.Upstream;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Infrastructure.Upstream;

/// <summary>Passes the caller's bearer token on to the upstream service so its authorization still applies.</summary>
public sealed class BearerForwardingHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authorization = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

internal static class UpstreamJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

public sealed class HttpValidationGateway(HttpClient client) : IValidationGateway
{
    public Task<ValidationRunSummary?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default) =>
        RunAsync($"internal/v1/validation/runs/{runId}", cancellationToken);

    public Task<ValidationRunSummary?> GetLatestRunAsync(CancellationToken cancellationToken = default) =>
        RunAsync("internal/v1/validation/runs/latest", cancellationToken);

    public async Task<IReadOnlyList<AssessedAddress>> GetAssessmentsAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            new Uri($"internal/v1/validation/runs/{runId}/assessments", UriKind.Relative),
            cancellationToken);

        EnsureReadable(response);

        var assessments = await response.Content.ReadFromJsonAsync<List<AssessmentResponse>>(
            UpstreamJson.Options,
            cancellationToken);

        return assessments is null
            ? []
            : [.. assessments.Select(assessment => new AssessedAddress(
                assessment.Id,
                assessment.SourceCode,
                assessment.SchemeCode,
                assessment.MessageId,
                assessment.EndToEndId,
                Enum.TryParse<PartyRole>(assessment.PartyRole, ignoreCase: true, out var role) ? role : PartyRole.Debtor,
                assessment.PartyName,
                assessment.Classification,
                assessment.CurrentOutcome,
                assessment.FutureOutcome,
                assessment.Country,
                assessment.TownName,
                assessment.PostCode,
                assessment.StreetName,
                assessment.BuildingNumber,
                assessment.AddressLines,
                assessment.EvidencePointer,
                [.. assessment.Issues.Select(issue => new AssessedIssue(
                    issue.Mode,
                    issue.RuleCode,
                    issue.Field,
                    issue.Severity,
                    issue.Message))]))];
    }

    private async Task<ValidationRunSummary?> RunAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        EnsureReadable(response);

        var run = await response.Content.ReadFromJsonAsync<RunResponse>(UpstreamJson.Options, cancellationToken);

        return run is null
            ? null
            : new ValidationRunSummary(
                run.Id,
                run.BatchId,
                run.SourceCode,
                run.SchemeCode,
                run.Status,
                run.AsOf,
                run.AssessedCount,
                run.PaymentsAtRisk);
    }

    private static void EnsureReadable(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new UpstreamException("validation", $"validation responded with {(int)response.StatusCode}.");
        }
    }

    private sealed record RunResponse(
        Guid Id,
        Guid BatchId,
        string SourceCode,
        string SchemeCode,
        string Status,
        DateOnly AsOf,
        int AssessedCount,
        int PaymentsAtRisk);

    private sealed record AssessmentResponse(
        Guid Id,
        string SourceCode,
        string? SchemeCode,
        string? MessageId,
        string? EndToEndId,
        string PartyRole,
        string? PartyName,
        string Classification,
        string CurrentOutcome,
        string FutureOutcome,
        string? Country,
        string? TownName,
        string? PostCode,
        string? StreetName,
        string? BuildingNumber,
        string? AddressLines,
        string EvidencePointer,
        List<IssueResponse> Issues);

    private sealed record IssueResponse(string Mode, string RuleCode, string Field, string Severity, string Message);
}

public sealed class HttpSourcesGateway(HttpClient client) : ISourcesGateway
{
    public async Task<SourceOwner?> GetOwnerAsync(string sourceCode, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            new Uri($"api/v1/sources/{Uri.EscapeDataString(sourceCode)}", UriKind.Relative),
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            // Ownership only enriches routing; a case is still worth opening without it.
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new UpstreamException("sources", $"sources responded with {(int)response.StatusCode}.");
        }

        var source = await response.Content.ReadFromJsonAsync<SourceResponse>(UpstreamJson.Options, cancellationToken);

        return source is null ? null : new SourceOwner(source.Code, source.OwnerName, source.OwnerEmail);
    }

    private sealed record SourceResponse(string Code, string? OwnerName, string? OwnerEmail);
}
