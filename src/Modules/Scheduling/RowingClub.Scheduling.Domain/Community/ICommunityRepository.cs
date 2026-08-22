namespace RowingClub.Scheduling.Domain.Community;

/// <summary>Akış modülünün tek deposu - post, beğeni, yorum, katılım ve takip birlikte yaşar.</summary>
public interface ICommunityRepository
{
    Task<List<Post>> GetFeedAsync(int take, CancellationToken cancellationToken);

    Task<Post?> GetPostAsync(Guid postId, CancellationToken cancellationToken);

    Task<PostMedia?> GetMediaAsync(Guid postId, CancellationToken cancellationToken);

    void AddPost(Post post, PostMedia? media);

    void RemovePost(Post post);

    Task<Dictionary<Guid, int>> GetLikeCountsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetLikedPostIdsAsync(Guid customerId, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<PostLike?> GetLikeAsync(Guid postId, Guid customerId, CancellationToken cancellationToken);

    void AddLike(PostLike like);

    void RemoveLike(PostLike like);

    Task<Dictionary<Guid, int>> GetCommentCountsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<List<PostComment>> GetCommentsAsync(Guid postId, CancellationToken cancellationToken);

    void AddComment(PostComment comment);

    Task<Dictionary<Guid, int>> GetParticipantCountsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetJoinedPostIdsAsync(Guid customerId, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken);

    Task<List<EventParticipation>> GetParticipantsAsync(Guid postId, CancellationToken cancellationToken);

    Task<EventParticipation?> GetParticipationAsync(Guid postId, Guid customerId, CancellationToken cancellationToken);

    void AddParticipation(EventParticipation participation);

    void RemoveParticipation(EventParticipation participation);

    Task<Follow?> GetFollowAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetFollowedIdsAsync(Guid followerId, CancellationToken cancellationToken);

    Task<int> GetFollowerCountAsync(Guid customerId, CancellationToken cancellationToken);

    void AddFollow(Follow follow);

    void RemoveFollow(Follow follow);
}
