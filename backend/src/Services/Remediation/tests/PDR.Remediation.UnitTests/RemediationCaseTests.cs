using AwesomeAssertions;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.UnitTests;

/// <summary>The workflow rules that keep a correction accountable (FR-WF-002 to FR-WF-007).</summary>
public sealed class RemediationCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_case_cannot_be_submitted_without_a_proposal()
    {
        var subject = Open();

        var result = subject.Submit("maker", evidenceRequired: false, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("REMEDIATION.PROPOSAL_MISSING");
    }

    [Fact]
    public void A_correction_that_adds_new_data_needs_evidence_before_submission()
    {
        var subject = Open();
        subject.Propose(ProposalMethod.DeterministicParse, Address(), null, "maker", Now);

        var withoutEvidence = subject.Submit("maker", evidenceRequired: true, Now);
        withoutEvidence.IsFailure.Should().BeTrue();
        withoutEvidence.Error.Code.Should().Be("REMEDIATION.EVIDENCE_REQUIRED");

        subject.AddEvidence("Document", "DOC-1", null, "maker", Now);
        subject.Submit("maker", evidenceRequired: true, Now).IsSuccess.Should().BeTrue();
        subject.Status.Should().Be(CaseStatus.PendingApproval);
    }

    [Fact]
    public void The_maker_cannot_approve_their_own_correction()
    {
        var subject = Submitted("maker");

        var result = subject.Decide(DecisionType.Approve, "MAKER", null, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("REMEDIATION.MAKER_CANNOT_CHECK");
        subject.Status.Should().Be(CaseStatus.PendingApproval);
    }

    [Fact]
    public void A_rejection_needs_a_rationale()
    {
        var subject = Submitted("maker");

        subject.Decide(DecisionType.Reject, "checker", null, null, Now)
            .Error.Code.Should().Be("REMEDIATION.RATIONALE_REQUIRED");
    }

    [Fact]
    public void An_exception_must_be_time_bound_and_expires_back_into_exposure()
    {
        var subject = Submitted("maker");

        subject.Decide(DecisionType.GrantException, "checker", "Awaiting customer", null, Now)
            .Error.Code.Should().Be("REMEDIATION.EXCEPTION_NEEDS_EXPIRY");

        subject.Decide(
            DecisionType.GrantException,
            "checker",
            "Awaiting customer",
            new DateOnly(2026, 4, 1),
            Now).IsSuccess.Should().BeTrue();

        subject.IsExceptionExpired(new DateOnly(2026, 3, 31)).Should().BeFalse();
        subject.IsExceptionExpired(new DateOnly(2026, 4, 2)).Should().BeTrue();
    }

    [Fact]
    public void A_returned_case_can_be_reworked_and_resubmitted()
    {
        var subject = Submitted("maker");
        subject.Decide(DecisionType.Return, "checker", "Wrong town", null, Now);
        subject.Status.Should().Be(CaseStatus.Returned);

        subject.Propose(ProposalMethod.ManualEdit, Address(), "Corrected", "maker", Now).IsSuccess.Should().BeTrue();
        subject.Submit("maker", evidenceRequired: false, Now).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Only_an_approved_case_may_be_queued_for_write_back()
    {
        var subject = Submitted("maker");

        subject.QueueForWriteBack("system", Now).Error.Code.Should().Be("REMEDIATION.NOT_APPROVED");

        subject.Decide(DecisionType.Approve, "checker", null, null, Now);
        subject.QueueForWriteBack("system", Now).IsSuccess.Should().BeTrue();
        subject.Status.Should().Be(CaseStatus.WriteBackPending);
    }

    [Fact]
    public void Repeat_occurrences_fold_into_the_same_case_and_raise_its_priority()
    {
        var subject = Open();
        subject.RecordOccurrences(4, 4, "batch/1");
        subject.RecordOccurrences(3, 3, "batch/2");

        subject.Occurrences.Should().Be(7);
        subject.FutureExposure.Should().Be(7);
        subject.EvidencePointer.Should().Be("batch/1");

        subject.Prioritize(daysToCutover: 10, schemeIsCritical: true);
        subject.Priority.Should().Be(CasePriority.Critical);

        subject.Prioritize(daysToCutover: 200, schemeIsCritical: false);
        subject.Priority.Should().Be(CasePriority.Medium);
    }

    [Fact]
    public void A_rolled_back_case_is_no_longer_remediated()
    {
        var subject = Submitted("maker");
        subject.Decide(DecisionType.Approve, "checker", null, null, Now);
        subject.QueueForWriteBack("system", Now);
        subject.MarkRemediated("system", Now);

        subject.MarkRolledBack("Source reverted", "operator", Now);

        subject.Status.Should().Be(CaseStatus.RolledBack);
        subject.RemediatedAtUtc.Should().BeNull();
        subject.History.Should().Contain(entry => entry.Action == "RolledBack");
    }

    private static RemediationCase Open() =>
        RemediationCase.Open(
            new CaseSubject(
                "key-1",
                "cbs",
                "Data Team",
                "data@example.local",
                "Acme GmbH",
                PartyRole.Creditor,
                new OriginalAddress(null, null, null, null, null, "Hauptstrasse 12|10115 Berlin|Germany"),
                "ADDR-STRUCT-001",
                "SEPA",
                "batch/1"),
            Now);

    private static RemediationCase Submitted(string maker)
    {
        var subject = Open();
        subject.Propose(ProposalMethod.DeterministicParse, Address(), null, maker, Now);
        subject.Submit(maker, evidenceRequired: false, Now);
        return subject;
    }

    private static ProposedAddress Address() =>
        new(
            "DE",
            "Berlin",
            "10115",
            "Hauptstrasse",
            "12",
            new FieldConfidence(100m, 90m, 90m, 70m, 70m),
            null,
            null);
}
