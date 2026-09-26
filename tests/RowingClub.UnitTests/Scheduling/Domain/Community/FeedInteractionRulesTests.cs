using FluentAssertions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain.Community;

namespace RowingClub.UnitTests.Scheduling.Domain.Community;

public sealed class FeedInteractionRulesTests
{
    private readonly Guid _postId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();

    [Fact]
    public void Reacting_without_an_existing_reaction_adds_one()
    {
        var change = PostReaction.Apply(null, _postId, _customerId, "🎉");

        change.Kind.Should().Be(ReactionChangeKind.Added);
        change.Reaction!.Emoji.Should().Be("🎉");
        change.Reaction.CustomerId.Should().Be(_customerId);
        change.CurrentEmoji.Should().Be("🎉");
    }

    [Fact]
    public void Reacting_with_another_emoji_changes_the_existing_reaction()
    {
        var existing = PostReaction.Create(_postId, _customerId, "👍");

        var change = PostReaction.Apply(existing, _postId, _customerId, "❤️");

        change.Kind.Should().Be(ReactionChangeKind.Changed);
        change.Reaction.Should().BeSameAs(existing);
        existing.Emoji.Should().Be("❤️");
        change.CurrentEmoji.Should().Be("❤️");
    }

    [Fact]
    public void Reacting_with_the_same_emoji_removes_the_reaction()
    {
        var existing = PostReaction.Create(_postId, _customerId, "😂");

        var change = PostReaction.Apply(existing, _postId, _customerId, "😂");

        change.Kind.Should().Be(ReactionChangeKind.Removed);
        change.CurrentEmoji.Should().BeNull();
    }

    [Fact]
    public void Null_emoji_removes_an_existing_reaction_and_is_a_no_op_otherwise()
    {
        var existing = PostReaction.Create(_postId, null, "👏");

        PostReaction.Apply(existing, _postId, null, null).Kind.Should().Be(ReactionChangeKind.Removed);
        PostReaction.Apply(null, _postId, null, null).Kind.Should().Be(ReactionChangeKind.None);
    }

    [Theory]
    [InlineData("🔥")]
    [InlineData("")]
    [InlineData("like")]
    public void Emoji_outside_the_allowed_set_is_rejected(string emoji)
    {
        var act = () => PostReaction.Apply(null, _postId, _customerId, emoji);

        act.Should().Throw<DomainValidationException>().Which.ErrorCode.Should().Be("invalid_reaction");
    }

    [Fact]
    public void Bare_heart_is_normalized_to_the_allowed_heart()
    {
        PostReaction.NormalizeEmoji("❤").Should().Be("❤️");
    }

    [Fact]
    public void Club_reaction_has_no_customer()
    {
        var reaction = PostReaction.Create(_postId, null, "👍");

        reaction.IsClub.Should().BeTrue();
    }

    [Fact]
    public void Reply_to_a_top_level_comment_is_allowed()
    {
        var parent = PostComment.Create(_postId, _customerId, "Ana yorum");

        var reply = PostComment.Create(_postId, null, "Kulüp yanıtı", parent);

        reply.ParentCommentId.Should().Be(parent.Id);
        reply.IsClub.Should().BeTrue();
    }

    [Fact]
    public void Reply_to_a_reply_is_rejected()
    {
        var parent = PostComment.Create(_postId, _customerId, "Ana yorum");
        var reply = PostComment.Create(_postId, _customerId, "Yanıt", parent);

        var act = () => PostComment.Create(_postId, _customerId, "Yanıta yanıt", reply);

        act.Should().Throw<DomainValidationException>().Which.ErrorCode.Should().Be("invalid_parent");
    }

    [Fact]
    public void Reply_to_a_comment_of_another_post_is_rejected()
    {
        var parent = PostComment.Create(Guid.NewGuid(), _customerId, "Başka paylaşım");

        var act = () => PostComment.Create(_postId, _customerId, "Yanıt", parent);

        act.Should().Throw<DomainValidationException>().Which.ErrorCode.Should().Be("invalid_parent");
    }

    [Fact]
    public void Member_can_delete_only_own_comment_and_club_can_delete_any()
    {
        var comment = PostComment.Create(_postId, _customerId, "Yorum");

        comment.Invoking(c => c.EnsureCanBeDeletedBy(_customerId)).Should().NotThrow();
        comment.Invoking(c => c.EnsureCanBeDeletedBy(null)).Should().NotThrow();
        comment.Invoking(c => c.EnsureCanBeDeletedBy(Guid.NewGuid()))
            .Should().Throw<DomainException>().Which.ErrorCode.Should().Be("forbidden");
    }

    [Fact]
    public void Media_set_accepts_up_to_ten_images()
    {
        var items = Enumerable.Range(0, Post.MaxMediaItems).Select(_ => ("aGVsbG8=", (string?)"image/png")).ToList();

        var validated = PostMediaValidator.ValidateSet(items);

        validated.Should().HaveCount(Post.MaxMediaItems).And.OnlyContain(v => v.Kind == PostMediaKind.Image);
    }

    [Fact]
    public void Media_set_with_more_than_ten_items_is_rejected()
    {
        var items = Enumerable.Range(0, Post.MaxMediaItems + 1).Select(_ => ("aGVsbG8=", (string?)"image/png")).ToList();

        var act = () => PostMediaValidator.ValidateSet(items);

        act.Should().Throw<DomainValidationException>().Which.ErrorCode.Should().Be("media_too_many");
    }

    [Fact]
    public void Video_must_be_the_only_media_item()
    {
        var items = new List<(string, string?)> { ("aGVsbG8=", "video/mp4"), ("aGVsbG8=", "image/png") };

        var act = () => PostMediaValidator.ValidateSet(items);

        act.Should().Throw<DomainValidationException>().Which.ErrorCode.Should().Be("media_invalid_combination");
    }

    [Fact]
    public void Single_video_is_accepted()
    {
        var validated = PostMediaValidator.ValidateSet([("aGVsbG8=", "video/mp4")]);

        validated.Should().ContainSingle().Which.Kind.Should().Be(PostMediaKind.Video);
    }
}
