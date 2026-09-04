using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using PDR.Reporting.Application.Upstream;

namespace PDR.Reporting.Infrastructure.Upstream;

/// <summary>Where reporting reads from; every base URL is configurable per environment.</summary>
public sealed class UpstreamOptions
{
    public const string SectionName = "Upstream";

    public string ValidationBaseUrl { get; set; } = "http://localhost:5106";

    public string RemediationBaseUrl { get; set; } = "http://localhost:5107";

    public string SimulationBaseUrl { get; set; } = "http://localhost:5108";

    /// <summary>The cutover plan the executive and cutover dashboards report against.</summary>
    public string CutoverPlanCode { get; set; } = "CUTOVER-2026";

    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>Passes the caller's bearer token upstream so their permissions still apply there.</summary>
public sealed class BearerForwardingHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authorization = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

internal static class Upstream
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// A dashboard is still worth showing when one upstream has nothing yet or refuses the caller; the
    /// snapshot is then labelled partial rather than failing the whole request (FR-RPT-002).
    /// </summary>
    public static async Task<T?> GetAsync<T>(HttpClient client, string service, string path, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new UpstreamException(service, $"{service} responded with {(int)response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
    }
}

public sealed class HttpValidationGateway(HttpClient client) : IValidationGateway
{
    public async Task<ValidationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var readiness = await Upstream.GetAsync<ReadinessResponse>(
            client,
            "validation",
            "api/v1/validation/readiness",
            cancellationToken);

        if (readiness is null)
        {
            return ValidationSnapshot.Empty;
        }

        // Validation reports issues rather than a warning total; warnings are the non-blocking ones.
        var currentWarnings = readiness.TopIssues
            .Where(issue => issue.Severity == "Warning" && issue.Mode != "Future")
            .Sum(issue => issue.Count);
        var futureWarnings = readiness.TopIssues
            .Where(issue => issue.Severity == "Warning")
            .Sum(issue => issue.Count);

        return new ValidationSnapshot(
            readiness.AssessedCount,
            readiness.ExcludedCount,
            readiness.UnableToAssessCount,
            readiness.CurrentRejectedCount,
            readiness.FutureRejectedCount,
            currentWarnings,
            futureWarnings,
            readiness.PaymentsAtRisk,
            null,
            readiness.AsOfUtc);
    }

    public async Task<IReadOnlyList<ValidationProfileRow>> GetProfileAsync(
        string dimension,
        CancellationToken cancellationToken = default)
    {
        var profile = await Upstream.GetAsync<ProfileResponse>(
            client,
            "validation",
            $"api/v1/validation/profile?dimension={Uri.EscapeDataString(dimension)}",
            cancellationToken);

        return profile is null
            ? []
            : [.. profile.Rows.Select(row => new ValidationProfileRow(
                dimension,
                row.Key,
                row.RecordCount,
                row.CurrentRejectedCount,
                row.FutureRejectedCount,
                row.CurrentWarningCount,
                row.FutureWarningCount))];
    }

    private sealed record ReadinessResponse(
        int AssessedCount,
        int ExcludedCount,
        int UnableToAssessCount,
        int CurrentRejectedCount,
        int FutureRejectedCount,
        int PaymentsAtRisk,
        List<IssueResponse> TopIssues,
        DateTimeOffset AsOfUtc);

    private sealed record IssueResponse(string RuleCode, string Severity, string Mode, int Count);

    private sealed record ProfileResponse(List<ProfileRowResponse> Rows);

    private sealed record ProfileRowResponse(
        string Key,
        int RecordCount,
        int CurrentRejectedCount,
        int FutureRejectedCount,
        int CurrentWarningCount,
        int FutureWarningCount);
}

public sealed class HttpRemediationGateway(HttpClient client) : IRemediationGateway
{
    public async Task<RemediationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var funnel = await Upstream.GetAsync<FunnelResponse>(
            client,
            "remediation",
            "api/v1/remediation/funnel",
            cancellationToken);

        return funnel is null
            ? RemediationSnapshot.Empty
            : new RemediationSnapshot(
                funnel.TotalCases,
                funnel.OpenCases,
                funnel.Approved,
                funnel.Remediated,
                funnel.ExpiredExceptions,
                funnel.FutureExposureOpen,
                funnel.FutureExposureRemediated);
    }

    private sealed record FunnelResponse(
        int TotalCases,
        int OpenCases,
        int Approved,
        int Remediated,
        int ExpiredExceptions,
        int FutureExposureOpen,
        int FutureExposureRemediated);
}

/// <summary>
/// Reads the simulation lab's own summary rather than recomputing it, so the executive dashboard and the
/// go/no-go pack can never disagree (FR-RPT-001).
/// </summary>
public sealed class HttpSimulationGateway(HttpClient client, UpstreamOptions options) : ISimulationGateway
{
    public async Task<SimulationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var pack = await Upstream.GetAsync<PackResponse>(
            client,
            "simulation",
            $"api/v1/simulation/cutover/{Uri.EscapeDataString(options.CutoverPlanCode)}/go-no-go",
            cancellationToken);

        var runs = await Upstream.GetAsync<RunsResponse>(
            client,
            "simulation",
            "api/v1/simulation/runs?page=1&pageSize=1",
            cancellationToken);

        if (pack is null && (runs is null || runs.Items.Count == 0))
        {
            return SimulationSnapshot.Empty;
        }

        var latest = runs?.Items.FirstOrDefault();

        return new SimulationSnapshot(
            latest?.Id,
            latest?.ScenarioCode,
            latest?.CompletedAtUtc,
            latest?.RejectedCount ?? 0,
            latest?.PaymentsAtRisk ?? 0,
            latest?.ReadinessPercent ?? 0m,
            pack?.Recommendation,
            pack?.ResidualExposure ?? 0,
            pack?.EntryCriteriaOutstanding ?? 0,
            pack?.ExitCriteriaOutstanding ?? 0,
            pack?.WaivedCriteria ?? 0,
            pack?.OpenDefects ?? 0,
            pack?.UatMismatches ?? 0,
            pack?.TestCoveragePercent ?? 0m,
            latest?.RulesetVersion);
    }

    private sealed record PackResponse(
        string Recommendation,
        int ResidualExposure,
        int PaymentsAtRisk,
        int OpenCases,
        int ExpiredExceptions,
        int OpenDefects,
        decimal TestCoveragePercent,
        int UatMismatches,
        int EntryCriteriaOutstanding,
        int ExitCriteriaOutstanding,
        int WaivedCriteria);

    private sealed record RunsResponse(List<RunResponse> Items);

    private sealed record RunResponse(
        Guid Id,
        string ScenarioCode,
        int RejectedCount,
        int PaymentsAtRisk,
        decimal ReadinessPercent,
        string? RulesetVersion,
        DateTimeOffset? CompletedAtUtc);
}
