using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;

namespace PDR.Simulation.Domain.Cutover;

public enum CriterionKind
{
    Entry = 0,
    Exit = 1
}

public enum CriterionStatus
{
    Pending = 0,
    Met = 1,

    /// <summary>Accepted as not met, with a reason; a waiver never reads as met (FR-CUT-002).</summary>
    Waived = 2,
    Failed = 3
}

public enum ApprovalDecision
{
    Approved = 0,
    Rejected = 1
}

public enum GoNoGoRecommendation
{
    Go = 0,
    GoWithConditions = 1,
    NoGo = 2
}

/// <summary>
/// The plan for a scheme cutover: the criteria that must hold before and after, the freeze window, the
/// fallback and support arrangements, and the accountable sign-offs (FR-CUT-001, FR-CUT-004).
/// </summary>
public sealed class CutoverPlan : AggregateRoot
{
    private readonly List<CutoverCriterion> _criteria = [];
    private readonly List<CutoverApproval> _approvals = [];

    private CutoverPlan()
    {
    }

    private CutoverPlan(string code, string name, DateOnly cutoverDate, string owner)
    {
        Code = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(code), 32).ToUpperInvariant();
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 140);
        CutoverDate = cutoverDate;
        Owner = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(owner), 140);
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public DateOnly CutoverDate { get; private set; }

    public string Owner { get; private set; } = string.Empty;

    public DateOnly? FreezeFrom { get; private set; }

    public DateOnly? FreezeTo { get; private set; }

    public string? FallbackPlan { get; private set; }

    public string? SupportModel { get; private set; }

    public bool IsFrozen(DateOnly today) =>
        FreezeFrom is not null && FreezeTo is not null && today >= FreezeFrom && today <= FreezeTo;

    public IReadOnlyCollection<CutoverCriterion> Criteria => _criteria.AsReadOnly();

    public IReadOnlyCollection<CutoverApproval> Approvals => _approvals.AsReadOnly();

    public static CutoverPlan Create(string code, string name, DateOnly cutoverDate, string owner) =>
        new(code, name, cutoverDate, owner);

    public void SetOperationalPlan(DateOnly? freezeFrom, DateOnly? freezeTo, string? fallbackPlan, string? supportModel)
    {
        FreezeFrom = freezeFrom;
        FreezeTo = freezeTo;
        FallbackPlan = fallbackPlan is null ? null : Ensure.MaxLength(fallbackPlan, 1024);
        SupportModel = supportModel is null ? null : Ensure.MaxLength(supportModel, 1024);
    }

    public Result<CutoverCriterion> AddCriterion(
        string reference,
        CriterionKind kind,
        string description,
        string owner,
        bool isBlocking)
    {
        if (_criteria.Any(item => string.Equals(item.Reference, reference, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<CutoverCriterion>(CutoverErrors.DuplicateCriterion(reference));
        }

        var criterion = new CutoverCriterion(Id, reference, kind, description, owner, isBlocking);
        _criteria.Add(criterion);
        return Result.Success(criterion);
    }

    public Result RecordCriterionStatus(
        string reference,
        CriterionStatus status,
        string? evidenceReference,
        string? rationale,
        string actor,
        DateTimeOffset atUtc)
    {
        var criterion = _criteria.FirstOrDefault(item => string.Equals(item.Reference, reference, StringComparison.OrdinalIgnoreCase));
        if (criterion is null)
        {
            return Result.Failure(CutoverErrors.CriterionNotFound(reference));
        }

        return criterion.Record(status, evidenceReference, rationale, actor, atUtc);
    }

    /// <summary>
    /// A sign-off is recorded against the pack the approver saw, so a later change to the exposure does
    /// not silently inherit an old approval (FR-CUT-004).
    /// </summary>
    public Result Approve(
        string role,
        string approver,
        ApprovalDecision decision,
        string rationale,
        GoNoGoRecommendation recommendationAtSignOff,
        DateTimeOffset atUtc)
    {
        if (decision == ApprovalDecision.Approved && recommendationAtSignOff == GoNoGoRecommendation.NoGo)
        {
            return Result.Failure(CutoverErrors.CannotApproveNoGo);
        }

        var existing = _approvals.FirstOrDefault(item => string.Equals(item.Role, role, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            _approvals.Remove(existing);
        }

        _approvals.Add(new CutoverApproval(Id, role, approver, decision, rationale, recommendationAtSignOff, atUtc));
        return Result.Success();
    }

    /// <summary>
    /// The recommendation follows the criteria and the residual exposure: any failed blocking criterion or
    /// rejected sign-off is a no-go, anything merely pending or waived is a go with conditions.
    /// </summary>
    public GoNoGoRecommendation Recommend(int residualExposure, int openDefects, int expiredExceptions)
    {
        var blockingFailed = _criteria.Any(item => item.IsBlocking && item.Status == CriterionStatus.Failed);
        var rejected = _approvals.Any(item => item.Decision == ApprovalDecision.Rejected);

        if (blockingFailed || rejected || residualExposure > 0 || expiredExceptions > 0)
        {
            return GoNoGoRecommendation.NoGo;
        }

        var entryOutstanding = _criteria.Any(item =>
            item.Kind == CriterionKind.Entry && item.Status is CriterionStatus.Pending);
        var waived = _criteria.Any(item => item.Status == CriterionStatus.Waived);

        return entryOutstanding || waived || openDefects > 0
            ? GoNoGoRecommendation.GoWithConditions
            : GoNoGoRecommendation.Go;
    }
}

public sealed class CutoverCriterion : Entity
{
    private CutoverCriterion()
    {
    }

    internal CutoverCriterion(Guid planId, string reference, CriterionKind kind, string description, string owner, bool isBlocking)
    {
        PlanId = planId;
        Reference = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(reference), 64).ToUpperInvariant();
        Kind = kind;
        Description = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(description), 512);
        Owner = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(owner), 140);
        IsBlocking = isBlocking;
        Status = CriterionStatus.Pending;
    }

    public Guid PlanId { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public CriterionKind Kind { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string Owner { get; private set; } = string.Empty;

    public bool IsBlocking { get; private set; }

    public CriterionStatus Status { get; private set; }

    public string? EvidenceReference { get; private set; }

    public string? Rationale { get; private set; }

    public string? RecordedBy { get; private set; }

    public DateTimeOffset? RecordedAtUtc { get; private set; }

    internal Result Record(CriterionStatus status, string? evidenceReference, string? rationale, string actor, DateTimeOffset atUtc)
    {
        if (status == CriterionStatus.Met && string.IsNullOrWhiteSpace(evidenceReference))
        {
            return Result.Failure(CutoverErrors.EvidenceRequired(Reference));
        }

        if (status == CriterionStatus.Waived && string.IsNullOrWhiteSpace(rationale))
        {
            return Result.Failure(CutoverErrors.RationaleRequired(Reference));
        }

        Status = status;
        EvidenceReference = evidenceReference is null ? null : Ensure.MaxLength(evidenceReference, 512);
        Rationale = rationale is null ? null : Ensure.MaxLength(rationale, 1024);
        RecordedBy = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(actor), 140);
        RecordedAtUtc = atUtc;
        return Result.Success();
    }
}

public sealed class CutoverApproval : Entity
{
    private CutoverApproval()
    {
    }

    internal CutoverApproval(
        Guid planId,
        string role,
        string approver,
        ApprovalDecision decision,
        string rationale,
        GoNoGoRecommendation recommendationAtSignOff,
        DateTimeOffset atUtc)
    {
        PlanId = planId;
        Role = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(role), 140);
        Approver = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(approver), 140);
        Decision = decision;
        Rationale = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(rationale), 1024);
        RecommendationAtSignOff = recommendationAtSignOff;
        DecidedAtUtc = atUtc;
    }

    public Guid PlanId { get; private set; }

    public string Role { get; private set; } = string.Empty;

    public string Approver { get; private set; } = string.Empty;

    public ApprovalDecision Decision { get; private set; }

    public string Rationale { get; private set; } = string.Empty;

    public GoNoGoRecommendation RecommendationAtSignOff { get; private set; }

    public DateTimeOffset DecidedAtUtc { get; private set; }
}

public static class CutoverErrors
{
    public static Error NotFound(string code) =>
        Error.NotFound("CUTOVER.NOT_FOUND", $"Cutover plan '{code}' was not found.");

    public static Error Duplicate(string code) =>
        Error.Conflict("CUTOVER.DUPLICATE", $"Cutover plan '{code}' already exists.");

    public static Error DuplicateCriterion(string reference) =>
        Error.Conflict("CUTOVER.DUPLICATE_CRITERION", $"Criterion '{reference}' already exists in this plan.");

    public static Error CriterionNotFound(string reference) =>
        Error.NotFound("CUTOVER.CRITERION_NOT_FOUND", $"Criterion '{reference}' was not found in this plan.");

    public static Error EvidenceRequired(string reference) =>
        Error.Validation("CUTOVER.EVIDENCE_REQUIRED", $"Criterion '{reference}' needs an evidence reference before it can be met.");

    public static Error RationaleRequired(string reference) =>
        Error.Validation("CUTOVER.RATIONALE_REQUIRED", $"Waiving criterion '{reference}' needs a rationale.");

    public static readonly Error CannotApproveNoGo =
        Error.Conflict("CUTOVER.CANNOT_APPROVE_NO_GO", "The pack currently recommends no-go, so it cannot be signed off as approved.");
}
