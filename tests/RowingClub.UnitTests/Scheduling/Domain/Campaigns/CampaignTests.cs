using FluentAssertions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain.Campaigns;

namespace RowingClub.UnitTests.Scheduling.Domain.Campaigns;

public sealed class CampaignTests
{
    private static readonly Guid PackageId = Guid.NewGuid();
    private static readonly DateTimeOffset Starts = DateTimeOffset.UtcNow;
    private static readonly DateTimeOffset Ends = DateTimeOffset.UtcNow.AddDays(10);

    [Fact]
    public void Create_throws_invalid_campaign_window_when_end_is_before_start()
    {
        var act = () => Campaign.Create(PackageId, Ends, Starts, 50m, null, null);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_campaign_window");
    }

    [Fact]
    public void Create_throws_invalid_campaign_price_when_negative()
    {
        var act = () => Campaign.Create(PackageId, Starts, Ends, -1m, null, null);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_campaign_price");
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(11, null)]
    [InlineData(null, -1)]
    [InlineData(null, 11)]
    public void Create_throws_invalid_campaign_level_when_out_of_range(int? minLevel, int? maxLevel)
    {
        var act = () => Campaign.Create(PackageId, Starts, Ends, 50m, minLevel, maxLevel);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_campaign_level");
    }

    [Fact]
    public void Create_throws_invalid_campaign_level_when_min_greater_than_max()
    {
        var act = () => Campaign.Create(PackageId, Starts, Ends, 50m, 8, 3);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_campaign_level");
    }

    [Fact]
    public void Create_succeeds_with_no_level_restriction_meaning_all_members()
    {
        var campaign = Campaign.Create(PackageId, Starts, Ends, 50m, null, null);

        campaign.MinLevel.Should().BeNull();
        campaign.MaxLevel.Should().BeNull();
        campaign.IsVisibleToLevel(0).Should().BeTrue();
        campaign.IsVisibleToLevel(10).Should().BeTrue();
    }

    [Fact]
    public void IsVisibleToLevel_respects_min_and_max_bounds()
    {
        var campaign = Campaign.Create(PackageId, Starts, Ends, 50m, 3, 6);

        campaign.IsVisibleToLevel(2).Should().BeFalse();
        campaign.IsVisibleToLevel(3).Should().BeTrue();
        campaign.IsVisibleToLevel(6).Should().BeTrue();
        campaign.IsVisibleToLevel(7).Should().BeFalse();
    }

    [Fact]
    public void IsActiveAt_is_true_only_within_window()
    {
        var campaign = Campaign.Create(PackageId, Starts, Ends, 50m, null, null);

        campaign.IsActiveAt(Starts.AddDays(-1)).Should().BeFalse();
        campaign.IsActiveAt(Starts.AddDays(1)).Should().BeTrue();
        campaign.IsActiveAt(Ends.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void Update_applies_all_fields_when_valid()
    {
        var campaign = Campaign.Create(PackageId, Starts, Ends, 50m, null, null);
        var newStarts = Starts.AddDays(1);
        var newEnds = Ends.AddDays(1);

        campaign.Update(newStarts, newEnds, 75m, 2, 8);

        campaign.StartsAtUtc.Should().Be(newStarts);
        campaign.EndsAtUtc.Should().Be(newEnds);
        campaign.Price.Should().Be(75m);
        campaign.MinLevel.Should().Be(2);
        campaign.MaxLevel.Should().Be(8);
    }
}
