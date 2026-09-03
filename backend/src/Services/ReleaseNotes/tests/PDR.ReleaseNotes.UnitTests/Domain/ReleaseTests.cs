using AwesomeAssertions;
using PDR.ReleaseNotes.Domain.Releases;

namespace PDR.ReleaseNotes.UnitTests.Domain;

public sealed class ReleaseTests
{
    private static Release DraftWithEntry()
    {
        var release = Release.CreateDraft("1.0.0", "First", new DateOnly(2026, 9, 1), null);
        release.AddEntry(ReleaseEntryType.Feature, "Validation", "Rule engine", null, null, null);
        return release;
    }

    [Fact]
    public void Publish_sets_status_and_raises_event()
    {
        var release = DraftWithEntry();
        var publishedAt = DateTimeOffset.UtcNow;

        var result = release.Publish("alice", publishedAt);

        result.IsSuccess.Should().BeTrue();
        release.Status.Should().Be(ReleaseStatus.Published);
        release.PublishedBy.Should().Be("alice");
        release.PublishedAtUtc.Should().Be(publishedAt);
        release.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ReleasePublished>();
    }

    [Fact]
    public void Publish_is_rejected_without_entries()
    {
        var release = Release.CreateDraft("1.0.0", "Empty", new DateOnly(2026, 9, 1), null);

        var result = release.Publish("alice", DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReleaseErrors.NoEntries);
    }

    [Fact]
    public void Publishing_twice_is_rejected()
    {
        var release = DraftWithEntry();
        release.Publish("alice", DateTimeOffset.UtcNow);

        var result = release.Publish("bob", DateTimeOffset.UtcNow);

        result.Error.Should().Be(ReleaseErrors.AlreadyPublished);
    }

    [Fact]
    public void Published_release_cannot_be_edited()
    {
        var release = DraftWithEntry();
        release.Publish("alice", DateTimeOffset.UtcNow);
        var entryId = release.Entries[0].Id;

        release.UpdateDetails("1.0.1", "Renamed", new DateOnly(2026, 9, 2), null).Error
            .Should().Be(ReleaseErrors.PublishedIsImmutable);
        release.AddEntry(ReleaseEntryType.Fix, "Validation", "Late fix", null, null, null).Error
            .Should().Be(ReleaseErrors.PublishedIsImmutable);
        release.UpdateEntry(entryId, ReleaseEntryType.Fix, "Validation", "Changed", null, 0, null).Error
            .Should().Be(ReleaseErrors.PublishedIsImmutable);
        release.RemoveEntry(entryId).Error.Should().Be(ReleaseErrors.PublishedIsImmutable);
    }

    [Fact]
    public void Erratum_is_only_allowed_after_publication()
    {
        var release = DraftWithEntry();

        release.AddErratum("Validation", "Correction", null, null).Error
            .Should().Be(ReleaseErrors.ErrataRequirePublished);

        release.Publish("alice", DateTimeOffset.UtcNow);
        var erratum = release.AddErratum("Validation", "Correction", "Body", ["PDR-001"]);

        erratum.IsSuccess.Should().BeTrue();
        erratum.Value.IsErratum.Should().BeTrue();
        erratum.Value.References.Should().Equal("PDR-001");
    }

    [Fact]
    public void Entries_get_sequential_sort_order_when_unspecified()
    {
        var release = Release.CreateDraft("1.0.0", "First", new DateOnly(2026, 9, 1), null);

        release.AddEntry(ReleaseEntryType.Feature, "A", "one", null, null, null);
        release.AddEntry(ReleaseEntryType.Fix, "B", "two", null, null, null);

        release.Entries.Select(entry => entry.SortOrder).Should().Equal(0, 1);
    }

    [Fact]
    public void RemoveEntry_reports_unknown_entry()
    {
        var release = DraftWithEntry();
        var unknown = Guid.CreateVersion7();

        var error = release.RemoveEntry(unknown).Error;

        error.Code.Should().Be("RELEASE.ENTRY_NOT_FOUND");
        error.Message.Should().Contain(unknown.ToString());
    }
}
