using AwesomeAssertions;
using PDR.Sources.Domain.Inventory;

namespace PDR.Sources.UnitTests.Domain;

public sealed class SourceSystemTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private static SourceSystem Hub() => SourceSystem.Register(
        "hub-eu",
        "European payment hub",
        SourceKind.PaymentHub,
        InterfaceKind.Database,
        "Payments Operations",
        "ops@example.com",
        "EU-BANK-01",
        "sepa,cbpr",
        "Hourly",
        1000,
        100,
        isAuthoritative: false);

    [Fact]
    public void Register_normalises_code_and_scheme_codes()
    {
        var source = Hub();

        source.Code.Should().Be("HUB-EU");
        source.SchemeCodes.Should().Be("SEPA,CBPR");
        source.Status.Should().Be(OnboardingStatus.Registered);
        source.Mapping.Should().Be(MappingReadiness.NotStarted);
    }

    [Fact]
    public void Adding_the_first_mapping_moves_mapping_readiness_to_in_progress()
    {
        var source = Hub();

        source.AddMapping("PARTY.CITY", "PstlAdr/TwnNm", null, false, null).IsSuccess.Should().BeTrue();

        source.Mapping.Should().Be(MappingReadiness.InProgress);
        source.Mappings.Should().ContainSingle();
    }

    [Fact]
    public void The_same_attribute_cannot_be_mapped_twice_to_the_same_element()
    {
        var source = Hub();
        source.AddMapping("PARTY.CITY", "PstlAdr/TwnNm", null, false, null);

        var duplicate = source.AddMapping("party.city", "pstladr/twnnm", null, false, null);

        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Code.Should().Be("SOURCE.DUPLICATE_MAPPING");
    }

    [Fact]
    public void Removing_an_unknown_mapping_fails()
    {
        Hub().RemoveMapping(Guid.NewGuid()).Error.Code.Should().Be("SOURCE.MAPPING_NOT_FOUND");
    }

    [Fact]
    public void Replacing_lineage_renumbers_the_hops()
    {
        var source = Hub();

        source.ReplaceLineage(
        [
            ("Customer master", "Channel", null, null),
            ("Channel", "Payment hub", "API", null)
        ]);
        source.ReplaceLineage([("Customer master", "Payment hub", "API", null)]);

        source.Lineage.Should().ContainSingle();
        source.Lineage.Single().Sequence.Should().Be(1);
    }

    [Fact]
    public void Scan_coverage_outside_zero_to_hundred_is_rejected()
    {
        Hub().RecordScan(120m, Now).Error.Code.Should().Be("SOURCE.INVALID_SCAN_COVERAGE");
    }

    [Fact]
    public void Recording_a_scan_moves_a_registered_source_into_scanning()
    {
        var source = Hub();

        source.RecordScan(40m, Now).IsSuccess.Should().BeTrue();

        source.Status.Should().Be(OnboardingStatus.Scanning);
        source.LastScanAtUtc.Should().Be(Now);
    }

    [Fact]
    public void A_source_that_was_never_attested_is_overdue()
    {
        Hub().IsAttestationOverdue(Now, 90).Should().BeTrue();
    }

    [Fact]
    public void Attestation_expires_after_the_configured_interval()
    {
        var source = Hub();
        source.AddMapping("PARTY.CITY", "PstlAdr/TwnNm", null, false, null);
        source.Attest("owner@example.com", Now.AddDays(-100));

        source.IsAttestationOverdue(Now, 90).Should().BeTrue();
        source.IsAttestationOverdue(Now, 120).Should().BeFalse();
        source.Mappings.Single().LastReviewedAtUtc.Should().Be(Now.AddDays(-100));
    }

    [Fact]
    public void Readiness_rewards_fresh_coverage_ready_mappings_and_a_current_attestation()
    {
        var source = Hub();
        source.RecordScan(100m, Now.AddDays(-1));
        source.Attest("owner@example.com", Now.AddDays(-1));
        source.Update(
            source.Name,
            source.Kind,
            source.Interface,
            source.OwnerName,
            source.OwnerEmail,
            source.LegalEntity,
            source.SchemeCodes,
            source.Schedule,
            source.EstimatedPartyCount,
            source.RecurringInstructionCount,
            source.IsAuthoritative,
            OnboardingStatus.Ready,
            MappingReadiness.Ready,
            "Data Management",
            isActive: true);

        source.ReadinessScore(Now, 90, 30).Should().Be(100m);
    }

    [Fact]
    public void A_stale_scan_only_counts_half_and_a_missing_attestation_scores_nothing()
    {
        var source = Hub();
        source.RecordScan(100m, Now.AddDays(-90));

        // 0 mapping + 0 attestation + 100/4 coverage
        source.ReadinessScore(Now, 90, 30).Should().Be(25m);
    }

    [Fact]
    public void An_inactive_source_is_never_reported_as_attestation_overdue()
    {
        var source = Hub();
        source.Update(
            source.Name,
            source.Kind,
            source.Interface,
            source.OwnerName,
            source.OwnerEmail,
            source.LegalEntity,
            source.SchemeCodes,
            source.Schedule,
            source.EstimatedPartyCount,
            source.RecurringInstructionCount,
            source.IsAuthoritative,
            OnboardingStatus.Blocked,
            MappingReadiness.NotStarted,
            null,
            isActive: false);

        source.IsAttestationOverdue(Now, 90).Should().BeFalse();
    }
}
