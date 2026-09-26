using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Community;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Community;
using RowingClub.Scheduling.Domain.Customers;

namespace RowingClub.UnitTests.Scheduling.Application.Community;

public sealed class FeedInteractionHandlerTests
{
    private readonly ICommunityRepository _repository = Substitute.For<ICommunityRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Post _post = Post.Create(null, "Merhaba", PostMediaKind.None, null, false, null, null);

    public FeedInteractionHandlerTests()
    {
        _repository.GetPostAsync(_post.Id, Arg.Any<CancellationToken>()).Returns(_post);
        _repository.GetReactionCountsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ReactionCount>());
        _repository.GetCommentLikeCountsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());
    }

    [Fact]
    public async Task Reacting_adds_a_reaction_and_returns_the_sorted_summary()
    {
        _repository.GetReactionCountsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ReactionCount>
            {
                new(_post.Id, "👍", 1),
                new(_post.Id, "❤️", 3),
                new(_post.Id, "😮", 0),
            });

        var summary = await new ReactToPostCommandHandler(_repository, _unitOfWork)
            .Handle(new ReactToPostCommand(_post.Id, _customerId, "❤️"), CancellationToken.None);

        _repository.Received(1).AddReaction(Arg.Is<PostReaction>(r => r != null && r.Emoji == "❤️" && r.CustomerId == _customerId));
        summary.MyReaction.Should().Be("❤️");
        summary.Reactions.Select(r => r.Emoji).Should().Equal("❤️", "👍");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalid_reaction_is_rejected_before_anything_is_saved()
    {
        var act = () => new ReactToPostCommandHandler(_repository, _unitOfWork)
            .Handle(new ReactToPostCommand(_post.Id, null, "🔥"), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainValidationException>()).Which.ErrorCode.Should().Be("invalid_reaction");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Legacy_like_toggles_the_thumbs_up_reaction()
    {
        var existing = PostReaction.Create(_post.Id, _customerId, PostReaction.ThumbsUp);
        _repository.GetReactionAsync(_post.Id, _customerId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await new ToggleLikeCommandHandler(_repository, _unitOfWork)
            .Handle(new ToggleLikeCommand(_post.Id, _customerId), CancellationToken.None);

        _repository.Received(1).RemoveReaction(existing);
        result.Active.Should().BeFalse();
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task Legacy_like_replaces_another_emoji_with_thumbs_up()
    {
        var existing = PostReaction.Create(_post.Id, _customerId, "😂");
        _repository.GetReactionAsync(_post.Id, _customerId, Arg.Any<CancellationToken>()).Returns(existing);
        _repository.GetReactionCountsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ReactionCount> { new(_post.Id, PostReaction.ThumbsUp, 2) });

        var result = await new ToggleLikeCommandHandler(_repository, _unitOfWork)
            .Handle(new ToggleLikeCommand(_post.Id, _customerId), CancellationToken.None);

        existing.Emoji.Should().Be(PostReaction.ThumbsUp);
        result.Should().Be(new ToggleResult(true, 2));
    }

    [Fact]
    public async Task Comment_like_toggles_on_and_off()
    {
        var comment = PostComment.Create(_post.Id, _customerId, "Yorum");
        _repository.GetCommentAsync(comment.Id, Arg.Any<CancellationToken>()).Returns(comment);
        var handler = new ToggleCommentLikeCommandHandler(_repository, _unitOfWork);

        var on = await handler.Handle(new ToggleCommentLikeCommand(comment.Id, null), CancellationToken.None);

        on.Active.Should().BeTrue();
        _repository.Received(1).AddCommentLike(Arg.Is<CommentLike>(l => l != null && l.CommentId == comment.Id && l.CustomerId == null));

        var existing = CommentLike.Create(comment.Id, null);
        _repository.GetCommentLikeAsync(comment.Id, null, Arg.Any<CancellationToken>()).Returns(existing);

        var off = await handler.Handle(new ToggleCommentLikeCommand(comment.Id, null), CancellationToken.None);

        off.Active.Should().BeFalse();
        _repository.Received(1).RemoveCommentLike(existing);
    }

    [Fact]
    public async Task Member_can_delete_own_comment()
    {
        var comment = PostComment.Create(_post.Id, _customerId, "Yorum");
        _repository.GetCommentAsync(comment.Id, Arg.Any<CancellationToken>()).Returns(comment);
        _repository.GetRepliesAsync(comment.Id, Arg.Any<CancellationToken>()).Returns(new List<PostComment>());

        await new DeleteCommentCommandHandler(_repository, _unitOfWork)
            .Handle(new DeleteCommentCommand(comment.Id, _customerId), CancellationToken.None);

        _repository.Received(1).RemoveComment(comment);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Member_cannot_delete_someone_elses_comment()
    {
        var comment = PostComment.Create(_post.Id, Guid.NewGuid(), "Yorum");
        _repository.GetCommentAsync(comment.Id, Arg.Any<CancellationToken>()).Returns(comment);

        var act = () => new DeleteCommentCommandHandler(_repository, _unitOfWork)
            .Handle(new DeleteCommentCommand(comment.Id, _customerId), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.ErrorCode.Should().Be("forbidden");
        _repository.DidNotReceive().RemoveComment(Arg.Any<PostComment>());
    }

    [Fact]
    public async Task Club_deleting_a_top_level_comment_also_deletes_its_replies()
    {
        var parent = PostComment.Create(_post.Id, Guid.NewGuid(), "Ana yorum");
        var replies = new List<PostComment>
        {
            PostComment.Create(_post.Id, _customerId, "Yanıt 1", parent),
            PostComment.Create(_post.Id, null, "Yanıt 2", parent),
        };
        _repository.GetCommentAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);
        _repository.GetRepliesAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(replies);

        await new DeleteCommentCommandHandler(_repository, _unitOfWork)
            .Handle(new DeleteCommentCommand(parent.Id, null), CancellationToken.None);

        _repository.Received(1).RemoveComment(parent);
        _repository.Received(1).RemoveComment(replies[0]);
        _repository.Received(1).RemoveComment(replies[1]);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Club_reply_returns_a_club_authored_comment()
    {
        var parent = PostComment.Create(_post.Id, _customerId, "Ana yorum");
        _repository.GetCommentAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);

        var dto = await new AddCommentCommandHandler(_repository, _customerRepository, _unitOfWork)
            .Handle(new AddCommentCommand(_post.Id, null, "Teşekkürler", parent.Id, "Boğaziçi Kürek"), CancellationToken.None);

        dto.ParentId.Should().Be(parent.Id);
        dto.IsClub.Should().BeTrue();
        dto.AuthorCustomerId.Should().BeNull();
        dto.AuthorName.Should().Be("Boğaziçi Kürek");
        dto.IsMine.Should().BeTrue();
    }

    [Fact]
    public async Task Reply_to_a_missing_parent_is_rejected()
    {
        var act = () => new AddCommentCommandHandler(_repository, _customerRepository, _unitOfWork)
            .Handle(new AddCommentCommand(_post.Id, _customerId, "Yanıt", Guid.NewGuid()), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainValidationException>()).Which.ErrorCode.Should().Be("invalid_parent");
    }

    [Fact]
    public async Task Media_query_returns_the_item_at_the_requested_index()
    {
        var second = PostMedia.Create(_post.Id, "aGVsbG8=", "image/png", 1);
        _repository.GetMediaAsync(_post.Id, 1, Arg.Any<CancellationToken>()).Returns(second);
        var handler = new GetPostMediaQueryHandler(_repository);

        var found = await handler.Handle(new GetPostMediaQuery(_post.Id, ClubOnly: true, 1), CancellationToken.None);
        var missing = await handler.Handle(new GetPostMediaQuery(_post.Id, ClubOnly: false, 5), CancellationToken.None);
        var outOfRange = await handler.Handle(new GetPostMediaQuery(_post.Id, ClubOnly: false, -1), CancellationToken.None);

        found.Should().Be(new PostMediaDto("aGVsbG8=", "image/png"));
        missing.Should().BeNull();
        outOfRange.Should().BeNull();
    }

    [Fact]
    public async Task Creating_a_post_with_multiple_images_stores_them_in_order()
    {
        var media = new List<PostMediaInput> { new("aGVsbG8=", "image/png"), new("aGVsbG8=", "image/jpeg") };

        await new CreatePostCommandHandler(_repository, _unitOfWork).Handle(
            new CreatePostCommand(null, "Galeri", "bGVnYWN5", "image/gif", false, null, null, Media: media),
            CancellationToken.None);

        _repository.Received(1).AddPost(
            Arg.Is<Post>(p => p != null && p.MediaKind == PostMediaKind.Image && p.MediaContentType == "image/png"),
            Arg.Is<IReadOnlyCollection<PostMedia>>(m =>
                m != null && m.Count == 2 && m.Select(x => x.Order).SequenceEqual(new[] { 0, 1 })
                && m.Last().ContentType == "image/jpeg"));
    }
}
