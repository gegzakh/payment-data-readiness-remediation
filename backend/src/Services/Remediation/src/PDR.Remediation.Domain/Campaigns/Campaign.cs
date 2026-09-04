using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Domain.Campaigns;

/// <summary>
/// A bundle of cases handed to one internal team or corporate customer to work through by a date
/// (FR-WF-006). Progress is tracked on the campaign so an owner can be chased as a whole.
/// </summary>
public sealed class Campaign : AggregateRoot
{
    private Campaign()
    {
    }

    private Campaign(
        string code,
        string name,
        CampaignAudience audience,
        string assignee,
        DateOnly dueDate,
        string? description)
    {
        Code = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(code), 32).ToUpperInvariant();
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 140);
        Audience = audience;
        Assignee = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(assignee), 140);
        DueDate = dueDate;
        Description = description is null ? null : Ensure.MaxLength(description, 1024);
        Status = CampaignStatus.Draft;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public CampaignAudience Audience { get; private set; }

    public string Assignee { get; private set; } = string.Empty;

    public DateOnly DueDate { get; private set; }

    public string? Description { get; private set; }

    public CampaignStatus Status { get; private set; }

    public int CaseCount { get; private set; }

    public int RemediatedCount { get; private set; }

    public decimal CompletionPercent =>
        CaseCount == 0 ? 0m : Math.Round(RemediatedCount * 100m / CaseCount, 2);

    public bool IsOverdue(DateOnly today) => Status == CampaignStatus.Active && DueDate < today;

    public static Campaign Create(
        string code,
        string name,
        CampaignAudience audience,
        string assignee,
        DateOnly dueDate,
        string? description) =>
        new(code, name, audience, assignee, dueDate, description);

    public Result Activate()
    {
        if (Status != CampaignStatus.Draft)
        {
            return Result.Failure(CampaignErrors.NotDraft(Status));
        }

        if (CaseCount == 0)
        {
            return Result.Failure(CampaignErrors.Empty);
        }

        Status = CampaignStatus.Active;
        return Result.Success();
    }

    public Result Cancel(string reason)
    {
        if (Status is CampaignStatus.Completed or CampaignStatus.Cancelled)
        {
            return Result.Failure(CampaignErrors.Closed(Status));
        }

        Description = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(reason), 1024);
        Status = CampaignStatus.Cancelled;
        return Result.Success();
    }

    /// <summary>Recomputed from the cases themselves so the campaign can never drift from reality.</summary>
    public void RecordProgress(int caseCount, int remediatedCount)
    {
        CaseCount = Math.Max(caseCount, 0);
        RemediatedCount = Math.Clamp(remediatedCount, 0, CaseCount);

        if (Status == CampaignStatus.Active && CaseCount > 0 && RemediatedCount == CaseCount)
        {
            Status = CampaignStatus.Completed;
        }
    }
}

public static class CampaignErrors
{
    public static Error NotFound(string code) =>
        Error.NotFound("CAMPAIGN.NOT_FOUND", $"Campaign '{code}' was not found.");

    public static Error Duplicate(string code) =>
        Error.Conflict("CAMPAIGN.DUPLICATE", $"Campaign '{code}' already exists.");

    public static Error NotDraft(CampaignStatus status) =>
        Error.Conflict("CAMPAIGN.NOT_DRAFT", $"A campaign in state '{status}' cannot be activated.");

    public static Error Closed(CampaignStatus status) =>
        Error.Conflict("CAMPAIGN.CLOSED", $"A campaign in state '{status}' can no longer be changed.");

    public static readonly Error Empty =
        Error.Conflict("CAMPAIGN.EMPTY", "A campaign needs at least one case before it can be activated.");
}
