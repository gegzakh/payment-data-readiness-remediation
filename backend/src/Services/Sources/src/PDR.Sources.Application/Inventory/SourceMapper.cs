using PDR.BuildingBlocks.Core.Settings;
using PDR.Sources.Domain.Inventory;

namespace PDR.Sources.Application.Inventory;

/// <summary>Resolves the runtime-configurable thresholds the readiness view depends on.</summary>
public sealed class SourceReadinessPolicy(ISettingsReader settings)
{
    public const int DefaultAttestationIntervalDays = 90;
    public const int DefaultScanFreshnessDays = 30;

    public async Task<(int AttestationIntervalDays, int ScanFreshnessDays)> ResolveAsync(
        CancellationToken cancellationToken = default)
    {
        var attestation = await settings.GetAsync(
            SourcesSettingKeys.AttestationIntervalDays,
            DefaultAttestationIntervalDays,
            cancellationToken);

        var freshness = await settings.GetAsync(
            SourcesSettingKeys.ScanFreshnessDays,
            DefaultScanFreshnessDays,
            cancellationToken);

        return (attestation, freshness);
    }
}

public static class SourceMapper
{
    public static SourceSystemDto ToDto(
        this SourceSystem source,
        DateTimeOffset nowUtc,
        int attestationIntervalDays,
        int scanFreshnessDays) =>
        new(
            source.Id,
            source.Code,
            source.Name,
            source.Kind,
            source.Interface,
            source.OwnerName,
            source.OwnerEmail,
            source.LegalEntity,
            SplitSchemes(source.SchemeCodes),
            source.Schedule,
            source.EstimatedPartyCount,
            source.RecurringInstructionCount,
            source.IsAuthoritative,
            source.Status,
            source.Mapping,
            source.ScanCoveragePercent,
            source.LastScanAtUtc,
            source.LastAttestedAtUtc,
            source.LastAttestedBy,
            source.IsAttestationOverdue(nowUtc, attestationIntervalDays),
            source.ReadinessScore(nowUtc, attestationIntervalDays, scanFreshnessDays),
            source.RemediationOwner,
            source.IsActive,
            source.Mappings
                .OrderBy(mapping => mapping.TargetElement, StringComparer.Ordinal)
                .Select(mapping => new FieldMappingDto(
                    mapping.Id,
                    mapping.SourceAttribute,
                    mapping.TargetElement,
                    mapping.Transformation,
                    mapping.IsAuthoritative,
                    mapping.Notes,
                    mapping.LastReviewedAtUtc))
                .ToList(),
            source.Lineage
                .Select(step => new LineageStepDto(
                    step.Sequence,
                    step.FromNode,
                    step.ToNode,
                    step.Channel,
                    step.Description))
                .ToList());

    public static IReadOnlyList<string> SplitSchemes(string schemeCodes) =>
        schemeCodes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}
