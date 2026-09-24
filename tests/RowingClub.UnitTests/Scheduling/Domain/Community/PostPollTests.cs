using FluentAssertions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain.Community;

namespace RowingClub.UnitTests.Scheduling.Domain.Community;

public sealed class PostPollTests
{
    private static readonly DateOnly Today = new(2026, 9, 23);

    private static Post CreateClubPoll(DateOnly? closesOn = null) =>
        Post.Create(null, "Cumartesi antrenmanı kaçta olsun?", PostMediaKind.None, null,
            false, null, null, isPoll: true, pollClosesOn: closesOn);

    [Fact]
    public void Club_poll_is_created_with_close_date()
    {
        var post = CreateClubPoll(Today);

        post.IsPoll.Should().BeTrue();
        post.PollClosesOn.Should().Be(Today);
    }

    [Fact]
    public void Member_cannot_create_a_poll()
    {
        var act = () => Post.Create(Guid.NewGuid(), "Soru?", PostMediaKind.None, null,
            false, null, null, isPoll: true);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("poll_club_only");
    }

    [Fact]
    public void Post_cannot_be_both_event_and_poll()
    {
        var act = () => Post.Create(null, "Soru?", PostMediaKind.None, null,
            true, "Regatta", Today, isPoll: true);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("poll_event_conflict");
    }

    [Fact]
    public void Poll_requires_a_question()
    {
        var act = () => Post.Create(null, "   ", PostMediaKind.Image, "image/png",
            false, null, null, isPoll: true);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("poll_question_required");
    }

    [Fact]
    public void Non_poll_post_ignores_poll_close_date()
    {
        var post = Post.Create(null, "Duyuru", PostMediaKind.None, null, false, null, null, pollClosesOn: Today);

        post.IsPoll.Should().BeFalse();
        post.PollClosesOn.Should().BeNull();
    }

    [Theory]
    [InlineData("Tek")]
    [InlineData("1|2|3|4|5|6|7")]
    [InlineData("Sabah|sabah ")]
    [InlineData("Sabah|  ")]
    public void Invalid_option_sets_are_rejected(string joinedTexts)
    {
        var act = () => PollOption.CreateSet(Guid.NewGuid(), joinedTexts.Split('|'));

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("poll_options_invalid");
    }

    [Fact]
    public void Option_longer_than_limit_is_rejected()
    {
        var act = () => PollOption.CreateSet(Guid.NewGuid(), ["Kısa", new string('x', PollOption.MaxTextLength + 1)]);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("poll_options_invalid");
    }

    [Fact]
    public void Valid_options_are_trimmed_and_ordered()
    {
        var postId = Guid.NewGuid();

        var options = PollOption.CreateSet(postId, ["  Sabah ", "Akşam", "Öğle"]);

        options.Select(o => o.Text).Should().Equal("Sabah", "Akşam", "Öğle");
        options.Select(o => o.Order).Should().Equal(0, 1, 2);
        options.Should().OnlyContain(o => o.PostId == postId);
        options.Select(o => o.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Poll_is_open_on_its_close_date_and_closed_the_day_after()
    {
        var post = CreateClubPoll(Today);

        post.IsPollClosed(Today).Should().BeFalse();
        post.IsPollClosed(Today.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void Poll_without_close_date_never_closes()
    {
        CreateClubPoll().IsPollClosed(Today.AddYears(5)).Should().BeFalse();
    }

    [Fact]
    public void Closing_early_sets_close_date_to_yesterday()
    {
        var post = CreateClubPoll(Today.AddDays(10));

        post.ClosePoll(Today);

        post.PollClosesOn.Should().Be(Today.AddDays(-1));
        post.IsPollClosed(Today).Should().BeTrue();
    }

    [Fact]
    public void Closing_an_already_closed_poll_keeps_the_original_date()
    {
        var post = CreateClubPoll(Today.AddDays(-7));

        post.ClosePoll(Today);

        post.PollClosesOn.Should().Be(Today.AddDays(-7));
    }

    [Fact]
    public void Closing_a_non_poll_is_rejected()
    {
        var post = Post.Create(null, "Duyuru", PostMediaKind.None, null, false, null, null);

        var act = () => post.ClosePoll(Today);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("not_a_poll");
    }
}
