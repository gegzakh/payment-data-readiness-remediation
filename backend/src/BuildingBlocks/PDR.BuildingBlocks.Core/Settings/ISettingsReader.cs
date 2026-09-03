namespace PDR.BuildingBlocks.Core.Settings;

/// <summary>
/// Read-only view over runtime configuration (database settings first, then environment/appsettings)
/// so application layers can resolve tunables without depending on persistence.
/// </summary>
public interface ISettingsReader
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);
}
