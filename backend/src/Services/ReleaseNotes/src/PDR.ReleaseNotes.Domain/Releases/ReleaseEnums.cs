namespace PDR.ReleaseNotes.Domain.Releases;

public enum ReleaseStatus
{
    Draft = 0,
    Published = 1
}

/// <summary>Logical grouping of a release entry as required by the release-notes page.</summary>
public enum ReleaseEntryType
{
    Feature = 0,
    Change = 1,
    Fix = 2,
    Security = 3,
    Breaking = 4
}
