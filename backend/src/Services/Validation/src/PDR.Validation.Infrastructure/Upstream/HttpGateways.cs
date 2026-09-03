using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using PDR.Validation.Application.Upstream;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Infrastructure.Upstream;

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

public sealed class HttpIngestionGateway(HttpClient client) : IIngestionGateway
{
    public async Task<IngestedBatch?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            new Uri($"internal/v1/batches/{batchId}", UriKind.Relative),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        EnsureReadable(response, "ingestion");

        var batch = await response.Content.ReadFromJsonAsync<IngestionBatchResponse>(
            UpstreamJson.Options,
            cancellationToken);

        return batch is null ? null : new IngestedBatch(batch.Id, batch.SourceCode, batch.Status, batch.ParsedCount);
    }

    public async Task<IReadOnlyList<IngestedRecord>> GetRecordsAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            new Uri($"internal/v1/batches/{batchId}/records", UriKind.Relative),
            cancellationToken);

        EnsureReadable(response, "ingestion");

        var records = await response.Content.ReadFromJsonAsync<List<IngestedRecord>>(
            UpstreamJson.Options,
            cancellationToken);

        return records ?? [];
    }

    private static void EnsureReadable(HttpResponseMessage response, string service)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new UpstreamException(service, $"{service} responded with {(int)response.StatusCode}.");
        }
    }

    private sealed record IngestionBatchResponse(Guid Id, string SourceCode, string Status, int ParsedCount);
}

public sealed class HttpRulesGateway(HttpClient client) : IRulesGateway
{
    public async Task<EffectiveRuleset?> GetEffectiveRulesetAsync(
        string schemeCode,
        DateOnly asOf,
        RuleMode mode,
        CancellationToken cancellationToken = default)
    {
        var uri = new Uri(
            $"api/v1/rulesets/effective?schemeCode={Uri.EscapeDataString(schemeCode)}" +
            $"&asOf={asOf.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&mode={mode}",
            UriKind.Relative);

        using var response = await client.GetAsync(uri, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new UpstreamException("rules", $"rules responded with {(int)response.StatusCode}.");
        }

        var ruleset = await response.Content.ReadFromJsonAsync<EffectiveRulesetResponse>(
            UpstreamJson.Options,
            cancellationToken);

        if (ruleset is null)
        {
            return null;
        }

        return new EffectiveRuleset(
            ruleset.SchemeCode,
            ruleset.VersionNumber,
            ruleset.AsOf,
            mode,
            [.. ruleset.Rules.Select(rule => new RuleSnapshot(
                rule.Code,
                rule.Field,
                Enum.TryParse<RuleCheck>(rule.Kind, ignoreCase: true, out var kind) ? kind : RuleCheck.Required,
                Severity(rule.Severity),
                rule.Message,
                rule.Parameter))]);
    }

    private static IssueSeverity Severity(string severity) => severity.ToUpperInvariant() switch
    {
        "ERROR" => IssueSeverity.Error,
        "WARNING" => IssueSeverity.Warning,
        _ => IssueSeverity.Info
    };

    private sealed record EffectiveRulesetResponse(
        string SchemeCode,
        int VersionNumber,
        DateOnly AsOf,
        List<RuleResponse> Rules);

    private sealed record RuleResponse(
        string Code,
        string Field,
        string Kind,
        string Severity,
        string Message,
        string? Parameter);
}
