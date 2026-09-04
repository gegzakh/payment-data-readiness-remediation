using AwesomeAssertions;
using PDR.Audit.Domain.Ledger;

namespace PDR.Audit.UnitTests.Domain;

public sealed class AuditRecordTests
{
    private static readonly DateTimeOffset Moment = new(2026, 9, 3, 10, 0, 0, TimeSpan.Zero);

    private static AuditRecord Append(AuditRecord? previous, string action = "ruleset.activated") =>
        AuditRecord.Append(
            previous,
            Moment,
            "rules",
            action,
            "Ruleset",
            "8b6bd1c0-0000-0000-0000-000000000001",
            "alice",
            "user-1",
            AuditOutcome.Success,
            "corr-1",
            "LE-01",
            "{\"version\":2}");

    [Fact]
    public void The_first_record_links_to_the_genesis_hash_at_sequence_one()
    {
        var record = Append(null);

        record.Sequence.Should().Be(1);
        record.PreviousHash.Should().Be(AuditRecord.GenesisHash);
        record.Hash.Should().HaveLength(64);
    }

    [Fact]
    public void Each_record_links_to_its_predecessor()
    {
        var first = Append(null);
        var second = Append(first, "ruleset.retired");

        second.Sequence.Should().Be(2);
        second.PreviousHash.Should().Be(first.Hash);
        second.Hash.Should().NotBe(first.Hash);
    }

    [Fact]
    public void Hashing_is_deterministic_for_identical_content()
    {
        Append(null).Hash.Should().Be(Append(null).Hash);
    }

    [Fact]
    public void Differing_content_produces_a_different_hash()
    {
        Append(null).Hash.Should().NotBe(Append(null, "ruleset.deleted").Hash);
    }

    [Fact]
    public void An_untouched_chain_verifies()
    {
        var first = Append(null);
        var second = Append(first, "scheme.updated");

        first.IsIntact(null).Should().BeTrue();
        second.IsIntact(first).Should().BeTrue();
    }

    [Fact]
    public void A_record_re_parented_to_a_different_predecessor_fails_verification()
    {
        var first = Append(null);
        var second = Append(first, "scheme.updated");
        var third = Append(second, "scheme.deleted");

        third.IsIntact(first).Should().BeFalse();
    }
}
