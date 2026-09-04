using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.BuildingBlocks.Persistence.Settings;
using PDR.Sources.Application.Inventory;
using PDR.Sources.Domain.Inventory;

namespace PDR.Sources.Infrastructure.Persistence;

/// <summary>
/// Seeds the attestation/freshness tunables plus a representative source inventory (payment hub, ERP
/// and customer master) so the readiness screens have a meaningful starting portfolio.
/// </summary>
public sealed class SourcesSeeder(SourcesDbContext context, IClock clock) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedSettingsAsync(cancellationToken);
        await SeedInventoryAsync(cancellationToken);
    }

    private async Task SeedSettingsAsync(CancellationToken cancellationToken)
    {
        var defaults = new (string Key, string Value, string Type, string Description)[]
        {
            (SourcesSettingKeys.AttestationIntervalDays,
                SourceReadinessPolicy.DefaultAttestationIntervalDays.ToString(),
                "int",
                "Days after which a source owner attestation is considered stale."),
            (SourcesSettingKeys.ScanFreshnessDays,
                SourceReadinessPolicy.DefaultScanFreshnessDays.ToString(),
                "int",
                "Days after which the last scan no longer counts as fresh coverage.")
        };

        foreach (var (key, value, type, description) in defaults)
        {
            if (!await context.SystemSettings.AnyAsync(setting => setting.Key == key, cancellationToken))
            {
                context.SystemSettings.Add(new SystemSetting(key, value, type, description));
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedInventoryAsync(CancellationToken cancellationToken)
    {
        if (await context.SourceSystems.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = clock.UtcNow;

        var hub = SourceSystem.Register(
            "HUB-EU",
            "European payment hub",
            SourceKind.PaymentHub,
            InterfaceKind.Database,
            "Payments Operations",
            "payments.ops@example.com",
            "EU-BANK-01",
            "SEPA,CBPR",
            "Hourly",
            1_250_000,
            84_000,
            isAuthoritative: false);

        hub.AddMapping("PARTY.ADDR_LINE_1", "PstlAdr/AdrLine[1]", null, false, "Free-format line still in use.");
        hub.AddMapping("PARTY.CITY", "PstlAdr/TwnNm", "Trim + title case", false, null);
        hub.AddMapping("PARTY.COUNTRY", "PstlAdr/Ctry", "ISO 3166-1 alpha-2", false, null);
        hub.ReplaceLineage(
        [
            ("Customer master", "Channel template", "Online banking", "Beneficiary details copied to template."),
            ("Channel template", "Payment hub", "Internal API", null),
            ("Payment hub", "pacs.008", "SEPA", "Message assembly from hub party record.")
        ]);
        hub.RecordScan(62.5m, now.AddDays(-4));
        hub.Attest("payments.ops@example.com", now.AddDays(-20));

        var erp = SourceSystem.Register(
            "ERP-AP",
            "Accounts payable ERP",
            SourceKind.Erp,
            InterfaceKind.Sftp,
            "Finance Systems",
            "finance.systems@example.com",
            "EU-BANK-01",
            "SEPA",
            "Daily 02:00",
            310_000,
            27_500,
            isAuthoritative: true);

        erp.AddMapping("SUPPLIER.ADDRESS", "PstlAdr/AdrLine[1]", "Split on newline", true, "Needs structured decomposition.");
        erp.AddMapping("SUPPLIER.POSTCODE", "PstlAdr/PstCd", null, true, null);
        erp.ReplaceLineage(
        [
            ("Supplier master", "AP payment file", "SFTP", "pain.001 batch generated nightly."),
            ("AP payment file", "Payment hub", "File transfer", null)
        ]);
        erp.RecordScan(18m, now.AddDays(-45));

        var master = SourceSystem.Register(
            "CRM-MDM",
            "Customer master data",
            SourceKind.MasterData,
            InterfaceKind.Api,
            "Data Management",
            "mdm.owner@example.com",
            "EU-BANK-02",
            "SEPA,CBPR,DOMESTIC",
            "Continuous",
            2_100_000,
            0,
            isAuthoritative: true);

        master.AddMapping("ADDRESS.STREET_NAME", "PstlAdr/StrtNm", null, true, null);
        master.AddMapping("ADDRESS.BUILDING_NUMBER", "PstlAdr/BldgNb", null, true, null);
        master.AddMapping("ADDRESS.TOWN", "PstlAdr/TwnNm", null, true, null);
        master.AddMapping("ADDRESS.POST_CODE", "PstlAdr/PstCd", null, true, null);
        master.AddMapping("ADDRESS.COUNTRY", "PstlAdr/Ctry", null, true, null);
        master.ReplaceLineage(
        [
            ("Customer onboarding", "Customer master", "API", "Structured address captured at onboarding."),
            ("Customer master", "Payment hub", "API", null)
        ]);
        master.RecordScan(94m, now.AddDays(-2));
        master.Attest("mdm.owner@example.com", now.AddDays(-5));
        master.Update(
            master.Name,
            master.Kind,
            master.Interface,
            master.OwnerName,
            master.OwnerEmail,
            master.LegalEntity,
            master.SchemeCodes,
            master.Schedule,
            master.EstimatedPartyCount,
            master.RecurringInstructionCount,
            master.IsAuthoritative,
            OnboardingStatus.Ready,
            MappingReadiness.Ready,
            "Data Management",
            isActive: true);

        context.SourceSystems.AddRange(hub, erp, master);
        await context.SaveChangesAsync(cancellationToken);
    }
}
