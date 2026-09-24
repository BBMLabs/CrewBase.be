using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Community;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Community;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.UnitTests.Scheduling.Application.Community;

public sealed class CastPollVoteCommandHandlerTests
{
    private readonly ICommunityRepository _repository = Substitute.For<ICommunityRepository>();
    private readonly ISettingsRepository _settingsRepository = Substitute.For<ISettingsRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();
    private readonly Guid _customerId = Guid.NewGuid();

    private CastPollVoteCommandHandler CreateHandler() => new(_repository, _settingsRepository, _unitOfWork);

    private (Post Post, List<PollOption> Options) ArrangePoll(DateOnly? closesOn = null)
    {
        var post = Post.Create(null, "Hangi gün?", PostMediaKind.None, null, false, null, null,
            isPoll: true, pollClosesOn: closesOn);
        var options = PollOption.CreateSet(post.Id, ["Cumartesi", "Pazar"]);

        _repository.GetPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        _repository.GetPollOptionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(options);
        _repository.GetPollVoteCountsByOptionAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());
        return (post, options);
    }

    private Task<PollDto> Vote(Post post, Guid optionId) =>
        CreateHandler().Handle(new CastPollVoteCommand(post.Id, _customerId, optionId), CancellationToken.None);

    [Fact]
    public async Task First_vote_is_recorded()
    {
        var (post, options) = ArrangePoll();
        _repository.GetPollVoteCountsByOptionAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [options[0].Id] = 1 });

        var poll = await Vote(post, options[0].Id);

        _repository.Received(1).AddPollVote(Arg.Is<PollVote>(
            v => v != null && v.PostId == post.Id && v.OptionId == options[0].Id && v.CustomerId == _customerId));
        poll.MyOptionId.Should().Be(options[0].Id);
        poll.TotalVotes.Should().Be(1);
        poll.Options.Select(o => o.VotedByMe).Should().Equal(true, false);
        poll.Closed.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Voting_the_same_option_again_removes_the_vote()
    {
        var (post, options) = ArrangePoll();
        var existing = PollVote.Create(post.Id, options[0].Id, _customerId);
        _repository.GetPollVoteAsync(post.Id, _customerId, Arg.Any<CancellationToken>()).Returns(existing);

        var poll = await Vote(post, options[0].Id);

        _repository.Received(1).RemovePollVote(existing);
        _repository.DidNotReceive().AddPollVote(Arg.Any<PollVote>());
        poll.MyOptionId.Should().BeNull();
        poll.Options.Should().OnlyContain(o => !o.VotedByMe);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Voting_another_option_changes_the_vote()
    {
        var (post, options) = ArrangePoll();
        var existing = PollVote.Create(post.Id, options[0].Id, _customerId);
        _repository.GetPollVoteAsync(post.Id, _customerId, Arg.Any<CancellationToken>()).Returns(existing);

        var poll = await Vote(post, options[1].Id);

        existing.OptionId.Should().Be(options[1].Id);
        _repository.DidNotReceive().RemovePollVote(Arg.Any<PollVote>());
        _repository.DidNotReceive().AddPollVote(Arg.Any<PollVote>());
        poll.MyOptionId.Should().Be(options[1].Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Closed_poll_rejects_votes()
    {
        var (post, options) = ArrangePoll(new DateOnly(2000, 1, 1));

        var act = () => Vote(post, options[0].Id);

        (await act.Should().ThrowAsync<DomainException>()).Which.ErrorCode.Should().Be("poll_closed");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Option_from_another_poll_is_rejected()
    {
        var (post, _) = ArrangePoll();

        var act = () => Vote(post, Guid.NewGuid());

        (await act.Should().ThrowAsync<DomainException>()).Which.ErrorCode.Should().Be("poll_option_invalid");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Non_poll_post_rejects_votes()
    {
        var post = Post.Create(null, "Duyuru", PostMediaKind.None, null, false, null, null);
        _repository.GetPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        _repository.GetPollOptionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<PollOption>());

        var act = () => Vote(post, Guid.NewGuid());

        (await act.Should().ThrowAsync<DomainException>()).Which.ErrorCode.Should().Be("not_a_poll");
    }

    [Fact]
    public async Task Missing_post_is_not_found()
    {
        var act = () => CreateHandler().Handle(
            new CastPollVoteCommand(Guid.NewGuid(), _customerId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
