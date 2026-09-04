using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Application.Cases;

public static class RemediationMapper
{
    public static CaseListItemDto ToListItem(this RemediationCase entity, DateOnly today) =>
        new(
            entity.Id,
            entity.CaseKey,
            entity.SourceCode,
            entity.PartyName,
            entity.PartyRole,
            entity.OriginalCountry,
            entity.IssueRuleCodes,
            entity.AffectedSchemes,
            entity.Occurrences,
            entity.FutureExposure,
            entity.Priority,
            entity.PriorityScore,
            entity.Status,
            entity.Queue,
            entity.AssignedTo,
            entity.DueDate,
            entity.IsOverdue(today),
            entity.Proposal?.OverallConfidence,
            entity.CampaignId,
            entity.OpenedAtUtc);

    public static CaseDetailDto ToDetail(this RemediationCase entity, DateOnly today) =>
        new(
            entity.Id,
            entity.CaseKey,
            entity.SourceCode,
            entity.OwnerName,
            entity.OwnerEmail,
            entity.PartyName,
            entity.PartyRole,
            new OriginalAddressDto(
                entity.OriginalCountry,
                entity.OriginalTownName,
                entity.OriginalPostCode,
                entity.OriginalStreetName,
                entity.OriginalBuildingNumber,
                entity.OriginalAddressLines),
            entity.Proposal?.ToDto(),
            entity.IssueRuleCodes,
            entity.AffectedSchemes,
            entity.EvidencePointer,
            entity.Occurrences,
            entity.FutureExposure,
            entity.Priority,
            entity.PriorityScore,
            entity.Status,
            entity.Queue,
            entity.AssignedTo,
            entity.DueDate,
            entity.IsOverdue(today),
            entity.CampaignId,
            entity.SubmittedBy,
            entity.SubmittedAtUtc,
            entity.DecidedBy,
            entity.DecidedAtUtc,
            entity.DecisionRationale,
            entity.ExceptionExpiresOn,
            entity.IsExceptionExpired(today),
            entity.FailureReason,
            entity.OpenedAtUtc,
            entity.RemediatedAtUtc,
            [.. entity.Evidence
                .OrderBy(evidence => evidence.CapturedAtUtc)
                .Select(evidence => new CaseEvidenceDto(
                    evidence.Id,
                    evidence.Kind,
                    evidence.Reference,
                    evidence.Description,
                    evidence.CapturedBy,
                    evidence.CapturedAtUtc))],
            [.. entity.History
                .OrderBy(item => item.OccurredAtUtc)
                .Select(item => new CaseEventDto(
                    item.Id,
                    item.Action,
                    item.FromStatus,
                    item.ToStatus,
                    item.Actor,
                    item.Rationale,
                    item.OccurredAtUtc))]);

    public static ProposalDto ToDto(this Proposal proposal) =>
        new(
            proposal.Id,
            proposal.Method,
            proposal.RequiresHumanVerification,
            proposal.Country,
            proposal.TownName,
            proposal.PostCode,
            proposal.StreetName,
            proposal.BuildingNumber,
            proposal.CountryConfidence,
            proposal.TownConfidence,
            proposal.PostCodeConfidence,
            proposal.StreetConfidence,
            proposal.BuildingNumberConfidence,
            proposal.OverallConfidence,
            proposal.Ambiguity,
            proposal.Alternatives,
            proposal.Notes);
}
