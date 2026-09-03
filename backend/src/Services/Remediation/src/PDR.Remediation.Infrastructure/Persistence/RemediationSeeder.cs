using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.BuildingBlocks.Persistence.Settings;
using PDR.Remediation.Application.Cases;
using PDR.Remediation.Application.WriteBack;
using PDR.Remediation.Domain.Cases;
using PDR.Remediation.Domain.WriteBack;

namespace PDR.Remediation.Infrastructure.Persistence;

/// <summary>
/// Seeds the remediation tunables and the write-back targets the local stack may write to. Cases only
/// ever come from real validation output.
/// </summary>
public sealed class RemediationSeeder(RemediationDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new (string Key, string Value, string Type, string Description)[]
        {
            (RemediationSettingKeys.PageSize,
                RemediationDefaults.PageSize.ToString(CultureInfo.InvariantCulture),
                "int",
                "Default page size for case and write-back listings."),
            (RemediationSettingKeys.BulkApprovalMinimumConfidence,
                RemediationDefaults.BulkApprovalMinimumConfidence.ToString(CultureInfo.InvariantCulture),
                "decimal",
                "Lowest overall proposal confidence a bulk submit or approval may include."),
            (RemediationSettingKeys.EvidenceRequiredForNewData,
                RemediationDefaults.EvidenceRequiredForNewData.ToString(),
                "bool",
                "Requires evidence before a proposal that introduces values absent from the source can be submitted."),
            (RemediationSettingKeys.SlaDays,
                RemediationDefaults.DefaultSlaDays.ToString(CultureInfo.InvariantCulture),
                "int",
                "Days a newly opened case has before it is overdue."),
            (RemediationSettingKeys.DefaultQueue,
                RemediationDefaults.DefaultQueue,
                "string",
                "Queue new cases are routed to when no rule applies."),
            (RemediationSettingKeys.CriticalSchemes,
                RemediationDefaults.CriticalSchemes,
                "string",
                "Comma-separated schemes whose exposure raises a case's priority."),
            (RemediationSettingKeys.CutoverDate,
                RemediationDefaults.CutoverDate,
                "string",
                "Date the post-cutover rules take effect; drives case urgency."),
            (WriteBackSettingKeys.ReadBackAfterWrite,
                WriteBackDefaults.ReadBackAfterWrite.ToString(),
                "bool",
                "Reads each corrected record back from the source before the case is marked remediated."),
            (WriteBackSettingKeys.MaxRecordsPerRun,
                WriteBackDefaults.MaxRecordsPerRun.ToString(CultureInfo.InvariantCulture),
                "int",
                "Upper bound on records written to a source in one job.")
        };

        foreach (var (key, value, type, description) in defaults)
        {
            if (!await context.SystemSettings.AnyAsync(setting => setting.Key == key, cancellationToken))
            {
                context.SystemSettings.Add(new SystemSetting(key, value, type, description));
            }
        }

        if (!await context.WriteBackTargets.AnyAsync(cancellationToken))
        {
            context.WriteBackTargets.Add(WriteBackTarget.Create(
                "CBS",
                WriteBackMode.Api,
                "country,town,postcode,street,buildingnumber",
                endpoint: "simulated://cbs/parties",
                exportFormat: null,
                maintenanceWindow: "Sun 01:00-03:00 UTC",
                maxRecordsPerRun: WriteBackDefaults.MaxRecordsPerRun,
                requiresApproval: true,
                rollbackMethod: "Reverse write of the stored original value"));

            context.WriteBackTargets.Add(WriteBackTarget.Create(
                "CRM",
                WriteBackMode.Export,
                "country,town,postcode",
                endpoint: null,
                exportFormat: "csv",
                maintenanceWindow: null,
                maxRecordsPerRun: 200,
                requiresApproval: true,
                rollbackMethod: "Counter-file with the original values"));
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
