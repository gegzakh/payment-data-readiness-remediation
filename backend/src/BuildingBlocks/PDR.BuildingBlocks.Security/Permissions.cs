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
        Settings.Read,
        Settings.Write
    ];
}
