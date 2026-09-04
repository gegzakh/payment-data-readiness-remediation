using AwesomeAssertions;
using PDR.Remediation.Domain.Campaigns;
using PDR.Remediation.Domain.Cases;

namespace PDR.Remediation.UnitTests;

/// <summary>Campaign progress is derived from its cases, never asserted by hand (FR-WF-006).</summary>
public sealed class CampaignTests
{
    [Fact]
    public void An_empty_campaign_cannot_be_activated()
    {
        var campaign = Draft();

        campaign.Activate().Error.Code.Should().Be("CAMPAIGN.EMPTY");
    }

    [Fact]
    public void Completion_follows_the_remediated_cases()
    {
        var campaign = Draft();
        campaign.RecordProgress(4, 0);
        campaign.Activate().IsSuccess.Should().BeTrue();

        campaign.RecordProgress(4, 1);
        campaign.CompletionPercent.Should().Be(25m);
        campaign.Status.Should().Be(CampaignStatus.Active);

        campaign.RecordProgress(4, 4);
        campaign.Status.Should().Be(CampaignStatus.Completed);
        campaign.CompletionPercent.Should().Be(100m);
    }

    [Fact]
    public void An_active_campaign_past_its_date_is_overdue()
    {
        var campaign = Draft();
        campaign.RecordProgress(1, 0);
        campaign.Activate();

        campaign.IsOverdue(new DateOnly(2026, 6, 1)).Should().BeFalse();
        campaign.IsOverdue(new DateOnly(2026, 7, 1)).Should().BeTrue();
    }

    private static Campaign Draft() =>
        Campaign.Create(
            "q2-corporates",
            "Q2 corporate address clean-up",
            CampaignAudience.CorporateCustomer,
            "Acme GmbH",
            new DateOnly(2026, 6, 30),
            null);
}
