namespace PDR.BuildingBlocks.Security;

/// <summary>
/// Permission constants shared by services and mirrored as Keycloak client roles.
/// Endpoints authorize on permissions, never on role names.
/// </summary>
public static class Permissions
{
    public static class ReleaseNotes
    {
        public const string Read = "releasenotes.read";
        public const string Write = "releasenotes.write";
        public const string Publish = "releasenotes.publish";
    }

    public static class Rules
    {
        public const string Read = "rules.read";
        public const string Write = "rules.write";
        public const string Activate = "rules.activate";
    }

    public static class Audit
    {
        public const string Read = "audit.read";
        public const string Write = "audit.write";
        public const string Verify = "audit.verify";
    }

    public static class Sources
    {
        public const string Read = "sources.read";
        public const string Write = "sources.write";
        public const string Attest = "sources.attest";
    }

    public static class Ingestion
    {
        public const string Read = "ingestion.read";
        public const string Write = "ingestion.write";
        public const string Manage = "ingestion.manage";
    }

    public static class Validation
    {
        public const string Read = "validation.read";
        public const string Run = "validation.run";

        /// <summary>Sees unmasked address values when drilling down from an aggregate (FR-VAL-009).</summary>
        public const string DrillDown = "validation.drilldown";
    }

    public static class Remediation
    {
        public const string Read = "remediation.read";

        /// <summary>Turns validation findings into cases and edits proposals as the maker (FR-WF-002).</summary>
        public const string Write = "remediation.write";

        /// <summary>Approves, returns, rejects or grants an exception as the checker (FR-WF-004).</summary>
        public const string Approve = "remediation.approve";

        /// <summary>Applies approved corrections to a source system and reverses them (FR-WB-003, FR-WB-007).</summary>
        public const string WriteBack = "remediation.writeback";
    }

    public static class Simulation
    {
        public const string Read = "simulation.read";

        /// <summary>Defines scenarios and executes simulation runs (FR-SIM-001).</summary>
        public const string Write = "simulation.write";
    }

    public static class Testing
    {
        public const string Read = "testing.read";

        /// <summary>Authors test plans and records executions, defects and UAT outcomes (FR-TST-001).</summary>
        public const string Write = "testing.write";
    }

    public static class Cutover
    {
        public const string Read = "cutover.read";

        /// <summary>Maintains the cutover plan, its criteria and the freeze window (FR-CUT-001).</summary>
        public const string Write = "cutover.write";

        /// <summary>Signs the go/no-go pack off on behalf of an accountable role (FR-CUT-004).</summary>
        public const string Approve = "cutover.approve";
    }

    public static class Reporting
    {
        public const string Read = "reporting.read";

        /// <summary>Exports a dashboard or a drill-down with its scope and freshness stamped on it (FR-RPT-002).</summary>
        public const string Export = "reporting.export";
    }

    public static class Notifications
    {
        public const string Read = "notifications.read";

        /// <summary>Manages own subscriptions and scheduled reports (FR-NTF-001).</summary>
        public const string Write = "notifications.write";

        /// <summary>Manages webhook endpoints, secrets and delivery retries (FR-API-002).</summary>
        public const string Admin = "notifications.admin";
    }

    public static class Settings
    {
        public const string Read = "settings.read";
        public const string Write = "settings.write";
    }

    public static IReadOnlyList<string> All { get; } =
    [
        ReleaseNotes.Read,
        ReleaseNotes.Write,
        ReleaseNotes.Publish,
        Rules.Read,
        Rules.Write,
        Rules.Activate,
        Audit.Read,
        Audit.Write,
        Audit.Verify,
        Sources.Read,
        Sources.Write,
        Sources.Attest,
        Ingestion.Read,
        Ingestion.Write,
        Ingestion.Manage,
        Validation.Read,
        Validation.Run,
        Validation.DrillDown,
        Remediation.Read,
        Remediation.Write,
        Remediation.Approve,
        Remediation.WriteBack,
        Simulation.Read,
        Simulation.Write,
        Testing.Read,
        Testing.Write,
        Cutover.Read,
        Cutover.Write,
        Cutover.Approve,
        Reporting.Read,
        Reporting.Export,
        Notifications.Read,
        Notifications.Write,
        Notifications.Admin,
        Settings.Read,
        Settings.Write
    ];
}
