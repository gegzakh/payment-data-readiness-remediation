namespace PDR.Remediation.Infrastructure.Upstream;

/// <summary>Where remediation reads its inputs from; configurable per environment.</summary>
public sealed class UpstreamOptions
{
    public const string SectionName = "Upstream";

    public string ValidationBaseUrl { get; set; } = "http://localhost:5106";

    public string SourcesBaseUrl { get; set; } = "http://localhost:5104";

    public int TimeoutSeconds { get; set; } = 60;
}
