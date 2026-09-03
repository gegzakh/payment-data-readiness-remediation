using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;
using PDR.Remediation.Application.Abstractions;
using PDR.Remediation.Domain.Campaigns;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.Application.Campaigns;

public sealed record CampaignDto(
    Guid Id,
    string Code,
    string Name,
    CampaignAudience Audience,
    string Assignee,
    DateOnly DueDate,
    string? Description,
    CampaignStatus Status,
    int CaseCount,
    int RemediatedCount,
    decimal CompletionPercent,
    bool IsOverdue);

public sealed record CreateCampaignCommand(
    string Code,
    string Name,
    CampaignAudience Audience,
    string Assignee,
    DateOnly DueDate,
    string? Description) : ICommand<CampaignDto>;

/// <summary>Puts a selection of cases into a campaign and activates it (FR-WF-006).</summary>
public sealed record AssignCasesToCampaignCommand(string Code, IReadOnlyList<Guid> CaseIds) : ICommand<CampaignDto>;

public sealed record GetCampaignsQuery : IQuery<IReadOnlyList<CampaignDto>>;

public sealed class CreateCampaignCommandValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(140);
        RuleFor(command => command.Assignee).NotEmpty().MaximumLength(140);
        RuleFor(command => command.Description).MaximumLength(1024);
    }
}

public sealed class AssignCasesToCampaignCommandValidator : AbstractValidator<AssignCasesToCampaignCommand>
{
    public AssignCasesToCampaignCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.CaseIds).NotEmpty();
    }
}

public static class CampaignMapper
{
    public static CampaignDto ToDto(this Campaign campaign, DateOnly today) =>
        new(
            campaign.Id,
            campaign.Code,
            campaign.Name,
            campaign.Audience,
            campaign.Assignee,
            campaign.DueDate,
            campaign.Description,
            campaign.Status,
            campaign.CaseCount,
            campaign.RemediatedCount,
            campaign.CompletionPercent,
            campaign.IsOverdue(today));
}

public sealed class CreateCampaignCommandHandler(IRemediationDbContext context, IClock clock)
    : IRequestHandler<CreateCampaignCommand, Result<CampaignDto>>
{
    public async Task<Result<CampaignDto>> HandleAsync(
        CreateCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var exists = await context.Campaigns.AnyAsync(campaign => campaign.Code == code, cancellationToken);
        if (exists)
        {
            return Result.Failure<CampaignDto>(CampaignErrors.Duplicate(code));
        }

        var campaign = Campaign.Create(
            code,
            request.Name,
            request.Audience,
            request.Assignee,
            request.DueDate,
            request.Description);

        context.Campaigns.Add(campaign);
        await context.SaveChangesAsync(cancellationToken);

        return campaign.ToDto(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
    }
}

public sealed class AssignCasesToCampaignCommandHandler(IRemediationDbContext context, IClock clock)
    : IRequestHandler<AssignCasesToCampaignCommand, Result<CampaignDto>>
{
    public async Task<Result<CampaignDto>> HandleAsync(
        AssignCasesToCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var campaign = await context.Campaigns.FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);
        if (campaign is null)
        {
            return Result.Failure<CampaignDto>(CampaignErrors.NotFound(code));
        }

        var cases = await context.Cases
            .Where(entity => request.CaseIds.Contains(entity.Id))
            .ToListAsync(cancellationToken);

        foreach (var entity in cases)
        {
            entity.JoinCampaign(campaign.Id);
        }

        var members = await context.Cases
            .Where(entity => entity.CampaignId == campaign.Id || request.CaseIds.Contains(entity.Id))
            .Select(entity => entity.Status)
            .ToListAsync(cancellationToken);

        campaign.RecordProgress(members.Count, members.Count(status => status == CaseStatus.Remediated));

        if (campaign.Status == CampaignStatus.Draft)
        {
            var activation = campaign.Activate();
            if (activation.IsFailure)
            {
                return Result.Failure<CampaignDto>(activation.Error);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return campaign.ToDto(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
    }
}

public sealed class GetCampaignsQueryHandler(IRemediationDbContext context, IClock clock)
    : IRequestHandler<GetCampaignsQuery, Result<IReadOnlyList<CampaignDto>>>
{
    public async Task<Result<IReadOnlyList<CampaignDto>>> HandleAsync(
        GetCampaignsQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        var campaigns = await context.Campaigns
            .AsNoTracking()
            .OrderBy(campaign => campaign.DueDate)
            .ToListAsync(cancellationToken);

        // Progress is derived from the cases so a campaign can never claim more than the queue shows.
        var progress = await context.Cases
            .AsNoTracking()
            .Where(entity => entity.CampaignId != null)
            .GroupBy(entity => entity.CampaignId!.Value)
            .Select(group => new
            {
                CampaignId = group.Key,
                Total = group.Count(),
                Remediated = group.Count(entity => entity.Status == CaseStatus.Remediated)
            })
            .ToListAsync(cancellationToken);

        foreach (var campaign in campaigns)
        {
            var counts = progress.FirstOrDefault(item => item.CampaignId == campaign.Id);
            campaign.RecordProgress(counts?.Total ?? 0, counts?.Remediated ?? 0);
        }

        return Result.Success<IReadOnlyList<CampaignDto>>([.. campaigns.Select(campaign => campaign.ToDto(today))]);
    }
}
