using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PDR.BuildingBlocks.Persistence.Settings;
using PDR.BuildingBlocks.Security;

namespace PDR.BuildingBlocks.WebApi;

public sealed record SettingDto(string Key, string Value, string ValueType, string? Description);

public sealed record UpdateSettingRequest(string Value);

/// <summary>
/// Runtime settings surface every service exposes, so tunables (e.g. release-notes page size)
/// can be changed without a redeploy. Sensitive values are never returned.
/// </summary>
public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var settings = app.MapGroup("/api/v1/settings").WithTags("Settings");

        settings.MapGet("/", async (ISettingsProvider provider, CancellationToken cancellationToken) =>
            {
                var all = await provider.GetAllAsync(cancellationToken);
                return Results.Ok(all
                    .Select(setting => new SettingDto(
                        setting.Key,
                        setting.IsSensitive ? "***" : setting.Value,
                        setting.ValueType,
                        setting.Description))
                    .ToList());
            })
            .RequireAuthorization(Permissions.Settings.Read)
            .WithName("GetSettings");

        settings.MapPut("/{key}", async (
                string key,
                UpdateSettingRequest request,
                ISettingsProvider provider,
                CancellationToken cancellationToken) =>
            {
                await provider.SetAsync(key, request.Value, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(Permissions.Settings.Write)
            .WithName("UpdateSetting");

        return app;
    }
}
