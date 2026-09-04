namespace PDR.BuildingBlocks.Core.Correlation;

public interface ICorrelationContext
{
    string CorrelationId { get; }
}

/// <summary>
/// Ambient correlation id, flowed across HTTP calls, messages and log/trace enrichment (NFR-007).
/// </summary>
public sealed class CorrelationContext : ICorrelationContext
{
    private static readonly AsyncLocal<string?> Current = new();

    public const string HeaderName = "X-Correlation-Id";

    public string CorrelationId => Current.Value ??= Guid.CreateVersion7().ToString("N");

    public static void Set(string correlationId) => Current.Value = correlationId;
}
