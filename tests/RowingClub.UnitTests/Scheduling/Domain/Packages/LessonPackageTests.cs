using FluentAssertions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.UnitTests.Scheduling.Domain.Packages;

public sealed class LessonPackageTests
{
    private static LessonPackage CreateValidPackage() =>
        LessonPackage.Create("Başlangıç Paketi", "Açıklama", 8, 100m, 30, null, null, null);

    [Fact]
    public void Create_throws_invalid_session_count_when_session_count_is_zero()
    {
        var act = () => LessonPackage.Create("Paket", null, 0, 100m, null, null, null);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_session_count");
    }

    [Fact]
    public void Create_throws_invalid_price_when_price_is_negative()
    {
        var act = () => LessonPackage.Create("Paket", null, 8, -1m, null, null, null);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_price");
    }

    [Fact]
    public void Create_throws_invalid_validity_days_when_zero_or_negative()
    {
        var act = () => LessonPackage.Create("Paket", null, 8, 100m, 0, null, null, null);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_validity_days");
    }

    [Fact]
    public void Create_allows_null_validity_days_for_unlimited_validity()
    {
        var package = LessonPackage.Create("Paket", null, 8, 100m, null, null, null);

        package.ValidityDays.Should().BeNull();
    }

    [Fact]
    public void Create_throws_invalid_campaign_window_when_end_is_before_start()
    {
        var starts = DateTimeOffset.UtcNow.AddDays(10);
        var ends = DateTimeOffset.UtcNow.AddDays(1);

        var act = () => LessonPackage.Create("Paket", null, 8, 100m, null, starts, ends);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_campaign_window");
    }

    [Fact]
    public void Create_throws_invalid_campaign_price_when_campaign_price_is_negative()
    {
        var starts = DateTimeOffset.UtcNow;
        var ends = DateTimeOffset.UtcNow.AddDays(10);

        var act = () => LessonPackage.Create("Paket", null, 8, 100m, null, starts, ends, campaignPrice: -1m);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_campaign_price");
    }

    [Fact]
    public void Create_throws_invalid_campaign_price_when_campaign_price_set_without_campaign_window()
    {
        var act = () => LessonPackage.Create("Paket", null, 8, 100m, null, null, null, campaignPrice: 50m);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_campaign_price");
    }

    [Fact]
    public void Create_succeeds_with_valid_campaign_window_and_price()
    {
        var starts = DateTimeOffset.UtcNow;
        var ends = DateTimeOffset.UtcNow.AddDays(10);

        var package = LessonPackage.Create("Paket", null, 8, 100m, null, starts, ends, campaignPrice: 50m);

        package.CampaignPrice.Should().Be(50m);
        package.CampaignStartsAtUtc.Should().Be(starts);
        package.CampaignEndsAtUtc.Should().Be(ends);
    }

    [Fact]
    public void Update_throws_invalid_session_count_when_session_count_is_negative()
    {
        var package = CreateValidPackage();

        var act = () => package.Update("Paket", null, -1, 100m, true, 30, null, null);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_session_count");
    }

    [Fact]
    public void Update_throws_invalid_price_when_price_is_negative()
    {
        var package = CreateValidPackage();

        var act = () => package.Update("Paket", null, 8, -5m, true, 30, null, null);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_price");
    }

    [Fact]
    public void Update_throws_invalid_validity_days_when_zero()
    {
        var package = CreateValidPackage();

        var act = () => package.Update("Paket", null, 8, 100m, true, 0, null, null);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_validity_days");
    }

    [Fact]
    public void Update_throws_invalid_campaign_window_when_end_equals_start()
    {
        var package = CreateValidPackage();
        var sameInstant = DateTimeOffset.UtcNow;

        var act = () => package.Update("Paket", null, 8, 100m, true, 30, sameInstant, sameInstant);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_campaign_window");
    }

    [Fact]
    public void Update_throws_invalid_campaign_price_when_negative()
    {
        var package = CreateValidPackage();
        var starts = DateTimeOffset.UtcNow;
        var ends = DateTimeOffset.UtcNow.AddDays(5);

        var act = () => package.Update("Paket", null, 8, 100m, true, 30, starts, ends, campaignPrice: -10m);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_campaign_price");
    }

    [Fact]
    public void Update_throws_invalid_campaign_price_when_set_without_campaign_window()
    {
        var package = CreateValidPackage();

        var act = () => package.Update("Paket", null, 8, 100m, true, 30, null, null, campaignPrice: 10m);

        var ex = act.Should().Throw<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_campaign_price");
    }

    [Fact]
    public void Update_applies_all_fields_when_valid()
    {
        var package = CreateValidPackage();

        package.Update("Yeni Ad", "Yeni Açıklama", 10, 200m, false, 60, null, null);

        package.Name.Should().Be("Yeni Ad");
        package.Description.Should().Be("Yeni Açıklama");
        package.SessionCount.Should().Be(10);
        package.Price.Should().Be(200m);
        package.IsActive.Should().BeFalse();
        package.ValidityDays.Should().Be(60);
    }
}
