using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Persistence.Migrations;
using PDR.BuildingBlocks.Persistence.Settings;
using PDR.Simulation.Application.Scenarios;
using PDR.Simulation.Domain.Cutover;
using PDR.Simulation.Domain.Scenarios;

namespace PDR.Simulation.Infrastructure.Persistence;

/// <summary>
/// Seeds the simulation tunables, the three standard scenarios every programme needs, and the default
/// cutover checklist. Runs, executions and sign-offs are only ever created by real activity.
/// </summary>
public sealed class SimulationSeeder(SimulationDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new (string Key, string Value, string Type, string Description)[]
        {
            (SimulationSettingKeys.PageSize,
                SimulationDefaults.PageSize.ToString(CultureInfo.InvariantCulture),
                "int",
                "Default page size for simulation run listings."),
            (SimulationSettingKeys.DefaultCutoverDate,
                SimulationDefaults.DefaultCutoverDate,
                "string",
                "Date the post-cutover rules take effect; the as-of date of a future scenario."),
            (SimulationSettingKeys.ResidualExposureTolerance,
                SimulationDefaults.ResidualExposureTolerance.ToString(CultureInfo.InvariantCulture),
                "int",
                "Rejections still tolerated at go-live before the pack recommends no-go.")
        };

        foreach (var (key, value, type, description) in defaults)
        {
            if (!await context.SystemSettings.AnyAsync(setting => setting.Key == key, cancellationToken))
            {
                context.SystemSettings.Add(new SystemSetting(key, value, type, description));
            }
        }

        var cutoverDate = DateOnly.ParseExact(
            SimulationDefaults.DefaultCutoverDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        if (!await context.Scenarios.AnyAsync(cancellationToken))
        {
            context.Scenarios.Add(Scenario.Create(
                "BASE-CURRENT",
                "Portfolio under today's rules",
                ScenarioMode.Current,
                DateOnly.FromDateTime(DateTime.UtcNow),
                description: "Baseline: what the payment schemes reject today."));

            context.Scenarios.Add(Scenario.Create(
                "BASE-FUTURE",
                "Portfolio under the post-cutover rules",
                ScenarioMode.Future,
                cutoverDate,
                description: "What the same population would do once structured addresses are mandatory."));

            context.Scenarios.Add(Scenario.Create(
                "BASE-REMEDIATED",
                "Portfolio after approved remediation lands",
                ScenarioMode.Remediated,
                cutoverDate,
                description: "The future world minus everything remediation has approved or written back."));
        }

        if (!await context.CutoverPlans.AnyAsync(cancellationToken))
        {
            var plan = CutoverPlan.Create("CUTOVER-2026", "Structured address cutover", cutoverDate, "Payments Programme");

            plan.SetOperationalPlan(
                cutoverDate.AddDays(-5),
                cutoverDate.AddDays(2),
                "Revert the source deployments and resume unstructured submission for the affected schemes.",
                "Hypercare: payments operations on point, data quality on standby, daily reject review.");

            AddCriterion(plan, "ENTRY-READINESS", CriterionKind.Entry, "Future-rules readiness at or above the agreed threshold.", "Data Quality", true);
            AddCriterion(plan, "ENTRY-EXCEPTIONS", CriterionKind.Entry, "No expired exceptions outstanding.", "Compliance", true);
            AddCriterion(plan, "ENTRY-TESTING", CriterionKind.Entry, "Risk-based test plan executed with no open defects.", "Test Manager", true);
            AddCriterion(plan, "ENTRY-SOURCES", CriterionKind.Entry, "Source deployments delivering structured addresses in production.", "Source Owners", true);
            AddCriterion(plan, "ENTRY-FREEZE", CriterionKind.Entry, "Change freeze agreed and communicated.", "Release Management", false);
            AddCriterion(plan, "EXIT-REJECTS", CriterionKind.Exit, "Reject rate back to the pre-cutover baseline.", "Payments Operations", true);
            AddCriterion(plan, "EXIT-RECURRENCE", CriterionKind.Exit, "No recurrence of remediated defects from any source.", "Data Quality", false);
            AddCriterion(plan, "EXIT-HYPERCARE", CriterionKind.Exit, "Hypercare closed with the support model handed to run.", "Payments Programme", false);

            context.CutoverPlans.Add(plan);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void AddCriterion(
        CutoverPlan plan,
        string reference,
        CriterionKind kind,
        string description,
        string owner,
        bool isBlocking) =>
        plan.AddCriterion(reference, kind, description, owner, isBlocking);
}
