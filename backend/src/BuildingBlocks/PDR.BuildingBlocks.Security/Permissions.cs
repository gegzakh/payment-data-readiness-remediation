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
        Settings.Read,
        Settings.Write
    ];
}
