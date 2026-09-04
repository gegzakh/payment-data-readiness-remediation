using AwesomeAssertions;
using PDR.ReleaseNotes.Application.Releases;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.UnitTests.Application;

public sealed class ReleaseMappingTests
{
    [Fact]
    public void Entries_are_grouped_by_component_and_ordered_by_type_then_sort_order()
    {
        var release = Release.CreateDraft("2.0.0", "Second", new DateOnly(2026, 10, 1), null);
        release.AddEntry(ReleaseEntryType.Fix, "Validation", "fix b", null, 1, null);
        release.AddEntry(ReleaseEntryType.Feature, "Validation", "feature a", null, 0, null);
        release.AddEntry(ReleaseEntryType.Feature, "Ingestion", "feature c", null, 2, null);

        var dto = release.ToDto();

        dto.Groups.Select(group => group.Component).Should().Equal("Ingestion", "Validation");
        dto.Groups.Single(group => group.Component == "Validation").Entries
            .Select(entry => entry.Title).Should().Equal("feature a", "fix b");
    }
}
