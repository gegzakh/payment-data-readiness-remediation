using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using PDR.BuildingBlocks.Core.Settings;

namespace PDR.BuildingBlocks.Persistence.Settings;

public interface ISettingsProvider : ISettingsReader
{
    Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
}

/// <summary>
/// Reads configuration with precedence: <c>system_settings</c> table → environment/appsettings.
/// Values are memoised briefly so hot paths do not hit the database per request.
/// </summary>
public sealed class SettingsProvider(
    BaseDbContext context,
    IConfiguration configuration,
    IMemoryCache cache) : ISettingsProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey(key), out string? cached))
        {
            return cached;
        }

        var stored = await context.SystemSettings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken);

        var value = stored ?? configuration[key];
        cache.Set(CacheKey(key), value, CacheDuration);
        return value;
    }

    public async Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        var raw = await GetAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        try
        {
            return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return defaultValue;
        }
    }

    public async Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.SystemSettings.AsNoTracking().OrderBy(setting => setting.Key).ToListAsync(cancellationToken);

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var setting = await context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (setting is null)
        {
            context.SystemSettings.Add(new SystemSetting(key, value, "string", description: null));
        }
        else
        {
            setting.Update(value);
        }

        await context.SaveChangesAsync(cancellationToken);
        cache.Remove(CacheKey(key));
    }

    private static string CacheKey(string key) => $"setting:{key}";
}
