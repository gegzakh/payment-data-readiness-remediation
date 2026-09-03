using AwesomeAssertions;
using PDR.Validation.Application.Assess;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.UnitTests;

public class AssessmentOutcomeTests
{
    private static AddressSnapshot Snapshot(bool isDuplicate = false, int sequence = 1) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CORE",
            sequence,
            "MSG-1",
            "E2E-1",
            PartyRole.Debtor,
            "Acme GmbH",
            "DE",
            "Berlin",
            "10115",
            "Invalidenstrasse",
            "12",
            null,
            "SEPA",
            isDuplicate,
            AddressClassification.Structured);

    [Fact]
    public void Records_without_findings_are_compliant()
    {
        var assessment = AddressAssessment.Create(Guid.NewGuid(), Snapshot());

        assessment.Conclude(true, true);

        assessment.CurrentOutcome.Should().Be(RecordOutcome.Compliant);
        assessment.FutureOutcome.Should().Be(RecordOutcome.Compliant);
    }

    [Fact]
    public void An_error_finding_rejects_only_its_own_mode()
    {
        var assessment = AddressAssessment.Create(Guid.NewGuid(), Snapshot());
        assessment.AddIssue(RuleMode.Future, "ADDR.STRUCT", "AddressLine", IssueSeverity.Error, "Must be structured.", null, null);

        assessment.Conclude(true, true);

        assessment.CurrentOutcome.Should().Be(RecordOutcome.Compliant);
        assessment.FutureOutcome.Should().Be(RecordOutcome.Rejected);
    }

    [Fact]
    public void Warnings_and_information_do_not_reject()
    {
        var assessment = AddressAssessment.Create(Guid.NewGuid(), Snapshot());
        assessment.AddIssue(RuleMode.Current, "ADDR.W", "PostCode", IssueSeverity.Warning, "Check.", null, null);
        assessment.AddIssue(RuleMode.Future, "ADDR.I", "PostCode", IssueSeverity.Info, "Note.", null, null);

        assessment.Conclude(true, true);

        assessment.CurrentOutcome.Should().Be(RecordOutcome.Warning);
        assessment.FutureOutcome.Should().Be(RecordOutcome.Informational);
    }

    [Fact]
    public void Duplicates_are_excluded_from_the_verdict()
    {
        var assessment = AddressAssessment.Create(Guid.NewGuid(), Snapshot(isDuplicate: true));

        assessment.Conclude(true, true);

        assessment.CurrentOutcome.Should().Be(RecordOutcome.Excluded);
        assessment.FutureOutcome.Should().Be(RecordOutcome.Excluded);
    }

    [Fact]
    public void A_missing_rule_set_makes_the_record_unable_to_assess()
    {
        var assessment = AddressAssessment.Create(Guid.NewGuid(), Snapshot());

        assessment.Conclude(false, true);

        assessment.CurrentOutcome.Should().Be(RecordOutcome.UnableToAssess);
        assessment.FutureOutcome.Should().Be(RecordOutcome.Compliant);
    }

    [Fact]
    public void Evidence_pointer_leads_back_to_the_batch_record()
    {
        var snapshot = Snapshot(sequence: 7);

        var assessment = AddressAssessment.Create(Guid.NewGuid(), snapshot);

        assessment.EvidencePointer.Should().Be($"batch:{snapshot.BatchId}#record:7");
    }

    [Fact]
    public void Run_counts_reconcile_and_readiness_excludes_duplicates()
    {
        var run = ValidationRun.Start(Guid.NewGuid(), "CORE", "SEPA", new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow);

        var compliant = AddressAssessment.Create(run.Id, Snapshot(sequence: 1));
        compliant.Conclude(true, true);

        var rejected = AddressAssessment.Create(run.Id, Snapshot(sequence: 2));
        rejected.AddIssue(RuleMode.Future, "ADDR.STRUCT", "AddressLine", IssueSeverity.Error, "Must be structured.", null, null);
        rejected.Conclude(true, true);

        var duplicate = AddressAssessment.Create(run.Id, Snapshot(isDuplicate: true, sequence: 3));
        duplicate.Conclude(true, true);

        run.Complete([compliant, rejected, duplicate], DateTimeOffset.UtcNow);

        run.InputRecordCount.Should().Be(3);
        run.AssessedCount.Should().Be(2);
        run.ExcludedCount.Should().Be(1);
        run.CurrentReadinessPercent.Should().Be(100m);
        run.FutureReadinessPercent.Should().Be(50m);
        run.PaymentsAtRisk.Should().Be(1);
        run.CountsReconcile().Should().BeTrue();
    }

    [Fact]
    public void Masked_projection_hides_address_detail_from_callers_without_drill_down()
    {
        var assessment = AddressAssessment.Create(Guid.NewGuid(), Snapshot());
        assessment.Conclude(true, true);

        var masked = assessment.ToDto(unmasked: false);
        var full = assessment.ToDto(unmasked: true);

        masked.StreetName.Should().NotBe(full.StreetName).And.StartWith("In");
        masked.PartyName.Should().NotBe("Acme GmbH");
        masked.Country.Should().Be("DE", "country drives reporting and is not personal data on its own");
        masked.Classification.Should().Be(full.Classification);
    }
}
