namespace PDR.Validation.Infrastructure.Upstream;

/// <summary>Where validation reads its inputs from; configurable per environment.</summary>
public sealed class UpstreamOptions
{
    public const string SectionName = "Upstream";

    public string IngestionBaseUrl { get; set; } = "http://localhost:5105";

    public string RulesBaseUrl { get; set; } = "http://localhost:5102";

    public int TimeoutSeconds { get; set; } = 30;
}
