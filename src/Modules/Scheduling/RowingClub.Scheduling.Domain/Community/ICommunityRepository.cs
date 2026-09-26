namespace RowingClub.Scheduling.Domain.Community;

public sealed record ReactionCount(Guid PostId, string Emoji, int Count);

/// <summary>Akış modülünün tek deposu - post, beğeni, yorum, katılım ve takip birlikte yaşar.</summary>
public interface ICommunityRepository
{
    Task<List<Post>> GetFeedAsync(int take, bool clubOnly, CancellationToken cancellationToken);

    Task<Post?> GetPostAsync(Guid postId, CancellationToken cancellationToken);

    Task<PostMedia?> GetMediaAsync(Guid postId, int order, CancellationToken cancellationToken);

    Task<Dictionary<Guid, int>> GetMediaCountsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    void AddPost(Post post, IReadOnlyCollection<PostMedia> media);

    void RemovePost(Post post);

    Task<List<ReactionCount>> GetReactionCountsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<Dictionary<Guid, string>> GetMyReactionsAsync(
        Guid? customerId, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<List<PostReaction>> GetReactionsAsync(Guid postId, CancellationToken cancellationToken);

    Task<PostReaction?> GetReactionAsync(Guid postId, Guid? customerId, CancellationToken cancellationToken);

    void AddReaction(PostReaction reaction);

    void RemoveReaction(PostReaction reaction);

    Task<Dictionary<Guid, int>> GetCommentCountsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<List<PostComment>> GetCommentsAsync(Guid postId, CancellationToken cancellationToken);

    Task<PostComment?> GetCommentAsync(Guid commentId, CancellationToken cancellationToken);

    Task<List<PostComment>> GetRepliesAsync(Guid parentCommentId, CancellationToken cancellationToken);

    void AddComment(PostComment comment);

    void RemoveComment(PostComment comment);

    Task<Dictionary<Guid, int>> GetCommentLikeCountsAsync(
        IReadOnlyCollection<Guid> commentIds, CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetLikedCommentIdsAsync(
        Guid? customerId, IReadOnlyCollection<Guid> commentIds, CancellationToken cancellationToken);

    Task<CommentLike?> GetCommentLikeAsync(Guid commentId, Guid? customerId, CancellationToken cancellationToken);

    void AddCommentLike(CommentLike like);

    void RemoveCommentLike(CommentLike like);

    Task<Dictionary<Guid, int>> GetParticipantCountsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetJoinedPostIdsAsync(Guid customerId, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<List<EventParticipation>> GetParticipantsAsync(Guid postId, CancellationToken cancellationToken);

    Task<EventParticipation?> GetParticipationAsync(Guid postId, Guid customerId, CancellationToken cancellationToken);

    void AddParticipation(EventParticipation participation);

    void RemoveParticipation(EventParticipation participation);

    void AddPollOptions(IEnumerable<PollOption> options);

    Task<List<PollOption>> GetPollOptionsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<Dictionary<Guid, int>> GetPollVoteCountsByOptionAsync(IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<Dictionary<Guid, Guid>> GetVotedOptionIdsAsync(Guid customerId, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<List<PollVote>> GetPollVotesAsync(Guid postId, CancellationToken cancellationToken);

    Task<PollVote?> GetPollVoteAsync(Guid postId, Guid customerId, CancellationToken cancellationToken);

    void AddPollVote(PollVote vote);

    void RemovePollVote(PollVote vote);

    Task<Follow?> GetFollowAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetFollowedIdsAsync(Guid followerId, CancellationToken cancellationToken);

    Task<int> GetFollowerCountAsync(Guid customerId, CancellationToken cancellationToken);

    void AddFollow(Follow follow);

    void RemoveFollow(Follow follow);
}
