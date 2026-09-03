using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;

namespace PDR.Remediation.Domain.Cases;

/// <summary>
/// One defective party address in one authoritative source, however many payments it appeared in
/// (FR-REM-001). It carries the proposal, the evidence, the maker-checker decisions and the full
/// history, and it is the only place the workflow rules live.
/// </summary>
public sealed class RemediationCase : AggregateRoot
{
    private readonly List<CaseEvidence> _evidence = [];
    private readonly List<CaseEvent> _history = [];

    private RemediationCase()
    {
    }

    private RemediationCase(CaseSubject subject, DateTimeOffset openedAtUtc)
    {
        CaseKey = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(subject.CaseKey), 128);
        SourceCode = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(subject.SourceCode), 32).ToUpperInvariant();
        OwnerName = Truncate(subject.OwnerName, 128);
        OwnerEmail = Truncate(subject.OwnerEmail, 256);
        PartyName = Truncate(subject.PartyName, 140);
        PartyRole = subject.PartyRole;
        OriginalCountry = Truncate(subject.Original.Country, 8);
        OriginalTownName = Truncate(subject.Original.TownName, 140);
        OriginalPostCode = Truncate(subject.Original.PostCode, 32);
        OriginalStreetName = Truncate(subject.Original.StreetName, 140);
        OriginalBuildingNumber = Truncate(subject.Original.BuildingNumber, 32);
        OriginalAddressLines = Truncate(subject.Original.AddressLines, 1024);
        IssueRuleCodes = Truncate(subject.IssueRuleCodes, 512) ?? string.Empty;
        AffectedSchemes = Truncate(subject.AffectedSchemes, 256) ?? string.Empty;
        EvidencePointer = Truncate(subject.EvidencePointer, 128) ?? string.Empty;
        Status = CaseStatus.New;
        Occurrences = 0;
        OpenedAtUtc = openedAtUtc;
    }

    /// <summary>Identity of the defect itself: source, party and address. Repeat payments fold into it.</summary>
    public string CaseKey { get; private set; } = string.Empty;

    public string SourceCode { get; private set; } = string.Empty;

    public string? OwnerName { get; private set; }

    public string? OwnerEmail { get; private set; }

    public string? PartyName { get; private set; }

    public PartyRole PartyRole { get; private set; }

    public string? OriginalCountry { get; private set; }

    public string? OriginalTownName { get; private set; }

    public string? OriginalPostCode { get; private set; }

    public string? OriginalStreetName { get; private set; }

    public string? OriginalBuildingNumber { get; private set; }

    public string? OriginalAddressLines { get; private set; }

    public string IssueRuleCodes { get; private set; } = string.Empty;

    public string AffectedSchemes { get; private set; } = string.Empty;

    public string EvidencePointer { get; private set; } = string.Empty;

    /// <summary>How many payment records the same defect produced (FR-REM-001).</summary>
    public int Occurrences { get; private set; }

    /// <summary>Occurrences the future rules would reject — the exposure this case removes.</summary>
    public int FutureExposure { get; private set; }

    public CasePriority Priority { get; private set; }

    public int PriorityScore { get; private set; }

    public string? Queue { get; private set; }

    public string? AssignedTo { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public Guid? CampaignId { get; private set; }

    public CaseStatus Status { get; private set; }

    public Proposal? Proposal { get; private set; }

    /// <summary>Who submitted the correction; the checker must be somebody else (FR-WF-003).</summary>
    public string? SubmittedBy { get; private set; }

    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    public string? DecidedBy { get; private set; }

    public DateTimeOffset? DecidedAtUtc { get; private set; }

    public string? DecisionRationale { get; private set; }

    /// <summary>An exception is time-bound and is never counted as compliant (FR-WF-007).</summary>
    public DateOnly? ExceptionExpiresOn { get; private set; }

    public DateTimeOffset OpenedAtUtc { get; private set; }

    public DateTimeOffset? RemediatedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<CaseEvidence> Evidence => _evidence.AsReadOnly();

    public IReadOnlyCollection<CaseEvent> History => _history.AsReadOnly();

    /// <summary>A granted exception that has run out is exposure again, not a pass (FR-WF-007).</summary>
    public bool IsExceptionExpired(DateOnly today) =>
        Status == CaseStatus.ExceptionGranted && ExceptionExpiresOn is { } expiry && expiry < today;

    public bool IsOverdue(DateOnly today) =>
        DueDate is { } due && due < today && Status is not (CaseStatus.Remediated or CaseStatus.Dismissed
            or CaseStatus.Rejected or CaseStatus.RolledBack);

    public static RemediationCase Open(CaseSubject subject, DateTimeOffset openedAtUtc)
    {
        var remediationCase = new RemediationCase(subject, openedAtUtc);
        remediationCase.Record("Opened", CaseStatus.New, "system", null, openedAtUtc);
        return remediationCase;
    }

    /// <summary>Folds another payment occurrence of the same defect into the case (FR-REM-001).</summary>
    public void RecordOccurrences(int occurrences, int futureExposure, string evidencePointer)
    {
        Occurrences += Math.Max(occurrences, 0);
        FutureExposure += Math.Max(futureExposure, 0);

        if (string.IsNullOrEmpty(EvidencePointer))
        {
            EvidencePointer = Truncate(evidencePointer, 128) ?? string.Empty;
        }
    }

    /// <summary>Ranks the case by what it costs to leave broken (FR-REM-006).</summary>
    public void Prioritize(int daysToCutover, bool schemeIsCritical)
    {
        var urgency = daysToCutover <= 0 ? 40 : daysToCutover <= 30 ? 30 : daysToCutover <= 90 ? 15 : 5;
        var volume = Math.Min(FutureExposure * 5, 40);
        var recurrence = Math.Min(Occurrences, 10);
        var criticality = schemeIsCritical ? 10 : 0;

        PriorityScore = urgency + volume + recurrence + criticality;
        Priority = PriorityScore switch
        {
            >= 75 => CasePriority.Critical,
            >= 50 => CasePriority.High,
            >= 25 => CasePriority.Medium,
            _ => CasePriority.Low
        };
    }

    /// <summary>Routes the case to a queue and owner with a due date (FR-WF-001).</summary>
    public void Assign(string queue, string? assignedTo, DateOnly? dueDate, string actor, DateTimeOffset atUtc)
    {
        Queue = Truncate(queue, 64);
        AssignedTo = Truncate(assignedTo, 128);
        DueDate = dueDate;

        if (Status == CaseStatus.New)
        {
            Status = CaseStatus.InProgress;
        }

        Record("Assigned", Status, actor, assignedTo, atUtc);
    }

    public void JoinCampaign(Guid campaignId) => CampaignId = campaignId;

    /// <summary>A maker writes or revises the proposed correction (FR-WF-002).</summary>
    public Result Propose(
        ProposalMethod method,
        ProposedAddress address,
        string? notes,
        string actor,
        DateTimeOffset atUtc)
    {
        if (Status is CaseStatus.Remediated or CaseStatus.Dismissed or CaseStatus.Rejected)
        {
            return Result.Failure(RemediationErrors.CaseClosed(Status));
        }

        if (Proposal is null)
        {
            Proposal = Proposal.Create(Id, method, address, notes);
        }
        else
        {
            Proposal.Revise(method, address, notes);
        }

        var from = Status;
        Status = CaseStatus.InProgress;
        Record("Proposed", from, actor, notes, atUtc);
        return Result.Success();
    }

    public Result AddEvidence(
        string kind,
        string reference,
        string? description,
        string actor,
        DateTimeOffset atUtc)
    {
        if (Status is CaseStatus.Remediated or CaseStatus.Dismissed)
        {
            return Result.Failure(RemediationErrors.CaseClosed(Status));
        }

        _evidence.Add(CaseEvidence.Create(Id, kind, reference, description, actor, atUtc));
        Record("EvidenceAdded", Status, actor, reference, atUtc);
        return Result.Success();
    }

    /// <summary>
    /// Sends the case to a checker. A proposal is mandatory, and so is evidence once the correction is
    /// not a plain restructuring of what the source already holds (FR-WF-004).
    /// </summary>
    public Result Submit(string maker, bool evidenceRequired, DateTimeOffset atUtc)
    {
        if (Status is not (CaseStatus.New or CaseStatus.InProgress or CaseStatus.Returned))
        {
            return Result.Failure(RemediationErrors.NotSubmittable(Status));
        }

        if (Proposal is null)
        {
            return Result.Failure(RemediationErrors.ProposalMissing);
        }

        if (evidenceRequired && _evidence.Count == 0)
        {
            return Result.Failure(RemediationErrors.EvidenceRequired);
        }

        var from = Status;
        SubmittedBy = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(maker), 128);
        SubmittedAtUtc = atUtc;
        Status = CaseStatus.PendingApproval;
        Record("Submitted", from, maker, null, atUtc);
        return Result.Success();
    }

    /// <summary>
    /// The checker's verdict. Four-eyes is enforced here: whoever submitted the correction cannot be the
    /// one who approves it (FR-WF-003).
    /// </summary>
    public Result Decide(
        DecisionType decision,
        string checker,
        string? rationale,
        DateOnly? exceptionExpiresOn,
        DateTimeOffset atUtc)
    {
        if (Status != CaseStatus.PendingApproval)
        {
            return Result.Failure(RemediationErrors.NotAwaitingDecision(Status));
        }

        if (string.Equals(checker, SubmittedBy, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(RemediationErrors.MakerCannotCheck);
        }

        if (decision is DecisionType.Return or DecisionType.Reject or DecisionType.Dismiss
            or DecisionType.GrantException && string.IsNullOrWhiteSpace(rationale))
        {
            return Result.Failure(RemediationErrors.RationaleRequired);
        }

        if (decision == DecisionType.GrantException && exceptionExpiresOn is null)
        {
            return Result.Failure(RemediationErrors.ExceptionNeedsExpiry);
        }

        var from = Status;
        Status = decision switch
        {
            DecisionType.Approve => CaseStatus.Approved,
            DecisionType.Return => CaseStatus.Returned,
            DecisionType.Reject => CaseStatus.Rejected,
            DecisionType.Dismiss => CaseStatus.Dismissed,
            _ => CaseStatus.ExceptionGranted
        };

        DecidedBy = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(checker), 128);
        DecidedAtUtc = atUtc;
        DecisionRationale = Truncate(rationale, 1024);
        ExceptionExpiresOn = decision == DecisionType.GrantException ? exceptionExpiresOn : null;

        Record(decision.ToString(), from, checker, rationale, atUtc);
        return Result.Success();
    }

    /// <summary>An approved case is queued for write-back to its owning source (FR-WB-003).</summary>
    public Result QueueForWriteBack(string actor, DateTimeOffset atUtc)
    {
        if (Status != CaseStatus.Approved)
        {
            return Result.Failure(RemediationErrors.NotApproved(Status));
        }

        Status = CaseStatus.WriteBackPending;
        Record("WriteBackQueued", CaseStatus.Approved, actor, null, atUtc);
        return Result.Success();
    }

    /// <summary>The source confirmed the corrected value, so the defect is gone (FR-WB-005).</summary>
    public void MarkRemediated(string actor, DateTimeOffset atUtc)
    {
        var from = Status;
        Status = CaseStatus.Remediated;
        RemediatedAtUtc = atUtc;
        FailureReason = null;
        Record("Remediated", from, actor, null, atUtc);
    }

    /// <summary>Write-back failed; the case stays open so the failure cannot pass as remediated (FR-WB-008).</summary>
    public void MarkFailed(string reason, string actor, DateTimeOffset atUtc)
    {
        var from = Status;
        Status = CaseStatus.Failed;
        FailureReason = Truncate(reason, 512);
        Record("WriteBackFailed", from, actor, reason, atUtc);
    }

    /// <summary>The applied correction was reversed in the source (FR-WB-007).</summary>
    public void MarkRolledBack(string reason, string actor, DateTimeOffset atUtc)
    {
        var from = Status;
        Status = CaseStatus.RolledBack;
        RemediatedAtUtc = null;
        FailureReason = Truncate(reason, 512);
        Record("RolledBack", from, actor, reason, atUtc);
    }

    private void Record(string action, CaseStatus from, string actor, string? rationale, DateTimeOffset atUtc) =>
        _history.Add(CaseEvent.Create(Id, action, from, Status, actor, rationale, atUtc));

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

/// <summary>Everything needed to open a case, handed to the aggregate as one value.</summary>
public sealed record CaseSubject(
    string CaseKey,
    string SourceCode,
    string? OwnerName,
    string? OwnerEmail,
    string? PartyName,
    PartyRole PartyRole,
    OriginalAddress Original,
    string IssueRuleCodes,
    string AffectedSchemes,
    string EvidencePointer);

/// <summary>The address exactly as the source and the message held it (FR-REM-002).</summary>
public sealed record OriginalAddress(
    string? Country,
    string? TownName,
    string? PostCode,
    string? StreetName,
    string? BuildingNumber,
    string? AddressLines);

public static class RemediationErrors
{
    public static Error CaseNotFound(Guid id) =>
        Error.NotFound("REMEDIATION.CASE_NOT_FOUND", $"Remediation case '{id}' was not found.");

    public static Error CaseClosed(CaseStatus status) =>
        Error.Conflict("REMEDIATION.CASE_CLOSED", $"A case in state '{status}' can no longer be edited.");

    public static Error NotSubmittable(CaseStatus status) =>
        Error.Conflict("REMEDIATION.NOT_SUBMITTABLE", $"A case in state '{status}' cannot be submitted.");

    public static Error NotAwaitingDecision(CaseStatus status) =>
        Error.Conflict("REMEDIATION.NOT_AWAITING_DECISION", $"A case in state '{status}' has nothing to decide.");

    public static Error NotApproved(CaseStatus status) =>
        Error.Conflict("REMEDIATION.NOT_APPROVED", $"Only an approved case can be written back; this one is '{status}'.");

    public static readonly Error ProposalMissing =
        Error.Conflict("REMEDIATION.PROPOSAL_MISSING", "The case has no proposed correction to review.");

    public static readonly Error EvidenceRequired =
        Error.Conflict("REMEDIATION.EVIDENCE_REQUIRED", "This correction needs supporting evidence before submission.");

    public static readonly Error MakerCannotCheck =
        Error.Forbidden("REMEDIATION.MAKER_CANNOT_CHECK", "The correction must be approved by somebody other than its maker.");

    public static readonly Error RationaleRequired =
        Error.Validation("REMEDIATION.RATIONALE_REQUIRED", "This decision requires a rationale.");

    public static readonly Error ExceptionNeedsExpiry =
        Error.Validation("REMEDIATION.EXCEPTION_NEEDS_EXPIRY", "An exception must be time-bound.");

    public static Error RunNotFound(Guid runId) =>
        Error.NotFound("REMEDIATION.RUN_NOT_FOUND", $"Validation run '{runId}' was not found.");

    public static Error UpstreamUnavailable(string service) =>
        Error.Dependency("REMEDIATION.UPSTREAM_UNAVAILABLE", $"The {service} service could not be reached.");
}
