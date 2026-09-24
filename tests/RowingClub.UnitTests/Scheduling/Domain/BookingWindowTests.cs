using FluentAssertions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.UnitTests.Scheduling.Domain;

public sealed class BookingWindowTests
{
    private static readonly CompanySettings Settings = CompanySettings.Default();

    private static (DateOnly Date, TimeOnly Time) HoursFromNow(double hours)
    {
        var at = Settings.NowLocal().AddHours(hours);
        return (DateOnly.FromDateTime(at), TimeOnly.FromDateTime(at));
    }

    [Fact]
    public void Effective_notice_is_never_below_24_hours()
    {
        Settings.MinNoticeHours.Should().BeLessThan(CompanySettings.BookingLeadHours);
        Settings.EffectiveNoticeHours.Should().Be(24);
    }

    [Fact]
    public void Cancellation_is_allowed_more_than_24_hours_before_start()
    {
        var (date, time) = HoursFromNow(25);

        var act = () => Settings.EnsureCancellable(date, time);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(23)]
    [InlineData(1)]
    [InlineData(-2)]
    public void Cancellation_is_rejected_within_24_hours_of_start(double hours)
    {
        var (date, time) = HoursFromNow(hours);

        var act = () => Settings.EnsureCancellable(date, time);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("cancel_too_late");
    }
}
