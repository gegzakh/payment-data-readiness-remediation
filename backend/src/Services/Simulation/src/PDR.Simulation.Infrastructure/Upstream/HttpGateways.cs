using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using PDR.Simulation.Application.Upstream;

namespace PDR.Simulation.Infrastructure.Upstream;

/// <summary>Where simulation reads its inputs from; configurable per environment.</summary>
public sealed class UpstreamOptions
{
    public const string SectionName = "Upstream";

    public string ValidationBaseUrl { get; set; } = "http://localhost:5106";

    public string RemediationBaseUrl { get; set; } = "http://localhost:5107";

    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>Passes the caller's bearer token on to the upstream service so its authorization still applies.</summary>
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

internal static class UpstreamJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

public sealed class HttpPortfolioGateway(HttpClient client) : IPortfolioGateway
{
    public async Task<PortfolioSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var readiness = await GetAsync<ReadinessResponse>("api/v1/validation/readiness", cancellationToken);

        return readiness is null
            ? new PortfolioSnapshot(0, 0, 0, 0, 0, 0, null, DateTimeOffset.UtcNow)
            : new PortfolioSnapshot(
                readiness.AssessedCount,
                readiness.ExcludedCount,
                readiness.UnableToAssessCount,
                readiness.CurrentRejectedCount,
                readiness.FutureRejectedCount,
                readiness.PaymentsAtRisk,
                null,
                readiness.AsOfUtc);
    }

    public async Task<IReadOnlyList<PortfolioProfileRow>> GetProfileAsync(
        string dimension,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetAsync<ProfileResponse>(
            $"api/v1/validation/profile?dimension={Uri.EscapeDataString(dimension)}",
            cancellationToken);

        return profile is null
            ? []
            : [.. profile.Rows.Select(row => new PortfolioProfileRow(
                dimension,
                row.Key,
                row.RecordCount,
                row.CurrentRejectedCount,
                row.FutureRejectedCount,
                row.CurrentWarningCount,
                row.FutureWarningCount))];
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new UpstreamException("validation", $"validation responded with {(int)response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<T>(UpstreamJson.Options, cancellationToken);
    }

    private sealed record ReadinessResponse(
        int AssessedCount,
        int ExcludedCount,
        int UnableToAssessCount,
        int CurrentRejectedCount,
        int FutureRejectedCount,
        int PaymentsAtRisk,
        DateTimeOffset AsOfUtc);

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
        using var response = await client.GetAsync(new Uri("api/v1/remediation/funnel", UriKind.Relative), cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            // A pack without remediation numbers is still worth producing; it simply shows nothing fixed.
            return new RemediationSnapshot(0, 0, 0, 0, 0, 0, 0);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new UpstreamException("remediation", $"remediation responded with {(int)response.StatusCode}.");
        }

        var funnel = await response.Content.ReadFromJsonAsync<FunnelResponse>(UpstreamJson.Options, cancellationToken);

        return funnel is null
            ? new RemediationSnapshot(0, 0, 0, 0, 0, 0, 0)
            : new RemediationSnapshot(
                funnel.TotalCases,
                funnel.Remediated,
                funnel.Approved,
                funnel.OpenCases,
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
