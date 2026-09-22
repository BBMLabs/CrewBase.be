using Microsoft.EntityFrameworkCore;
using RowingClub.Scheduling.Domain.Community;
using RowingClub.Scheduling.Infrastructure.Persistence;

namespace RowingClub.Scheduling.Infrastructure.Repositories;

public sealed class CommunityRepository(TenantDbContext context) : ICommunityRepository
{
    public Task<List<Post>> GetFeedAsync(int take, bool clubOnly, CancellationToken cancellationToken) =>
        (clubOnly ? context.Posts.Where(p => p.AuthorCustomerId == null) : context.Posts)
            .OrderByDescending(p => p.CreatedAtUtc).Take(take).ToListAsync(cancellationToken);

    public Task<Post?> GetPostAsync(Guid postId, CancellationToken cancellationToken) =>
        context.Posts.FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

    public Task<PostMedia?> GetMediaAsync(Guid postId, CancellationToken cancellationToken) =>
        context.Set<PostMedia>().FirstOrDefaultAsync(m => m.PostId == postId, cancellationToken);

    public void AddPost(Post post, PostMedia? media)
    {
        context.Posts.Add(post);
        if (media is not null)
            context.Set<PostMedia>().Add(media);
    }

    public void RemovePost(Post post) => context.Posts.Remove(post);

    public async Task<Dictionary<Guid, int>> GetLikeCountsAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken) =>
        (await context.PostLikes.Where(l => postIds.Contains(l.PostId))
            .GroupBy(l => l.PostId).Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.Key, x => x.Count);

    public async Task<HashSet<Guid>> GetLikedPostIdsAsync(
        Guid customerId, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken) =>
        (await context.PostLikes
            .Where(l => l.CustomerId == customerId && postIds.Contains(l.PostId))
            .Select(l => l.PostId).ToListAsync(cancellationToken)).ToHashSet();

    public Task<PostLike?> GetLikeAsync(Guid postId, Guid customerId, CancellationToken cancellationToken) =>
        context.PostLikes.FirstOrDefaultAsync(
            l => l.PostId == postId && l.CustomerId == customerId, cancellationToken);

    public void AddLike(PostLike like) => context.PostLikes.Add(like);

    public void RemoveLike(PostLike like) => context.PostLikes.Remove(like);

    public async Task<Dictionary<Guid, int>> GetCommentCountsAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken) =>
        (await context.PostComments.Where(c => postIds.Contains(c.PostId))
            .GroupBy(c => c.PostId).Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.Key, x => x.Count);

    public Task<List<PostComment>> GetCommentsAsync(Guid postId, CancellationToken cancellationToken) =>
        context.PostComments.Where(c => c.PostId == postId).ToListAsync(cancellationToken);

    public void AddComment(PostComment comment) => context.PostComments.Add(comment);

    public async Task<Dictionary<Guid, int>> GetParticipantCountsAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken) =>
        (await context.EventParticipations.Where(p => postIds.Contains(p.PostId))
            .GroupBy(p => p.PostId).Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.Key, x => x.Count);

    public async Task<HashSet<Guid>> GetJoinedPostIdsAsync(
        Guid customerId, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken) =>
        (await context.EventParticipations
            .Where(p => p.CustomerId == customerId && postIds.Contains(p.PostId))
            .Select(p => p.PostId).ToListAsync(cancellationToken)).ToHashSet();

    public Task<List<EventParticipation>> GetParticipantsAsync(Guid postId, CancellationToken cancellationToken) =>
        context.EventParticipations.Where(p => p.PostId == postId).ToListAsync(cancellationToken);

    public Task<EventParticipation?> GetParticipationAsync(
        Guid postId, Guid customerId, CancellationToken cancellationToken) =>
        context.EventParticipations.FirstOrDefaultAsync(
            p => p.PostId == postId && p.CustomerId == customerId, cancellationToken);

    public void AddParticipation(EventParticipation participation) =>
        context.EventParticipations.Add(participation);

    public void RemoveParticipation(EventParticipation participation) =>
        context.EventParticipations.Remove(participation);

    public Task<Follow?> GetFollowAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken) =>
        context.Follows.FirstOrDefaultAsync(
            f => f.FollowerId == followerId && f.FollowedId == followedId, cancellationToken);

    public async Task<HashSet<Guid>> GetFollowedIdsAsync(Guid followerId, CancellationToken cancellationToken) =>
        (await context.Follows.Where(f => f.FollowerId == followerId)
            .Select(f => f.FollowedId).ToListAsync(cancellationToken)).ToHashSet();

    public Task<int> GetFollowerCountAsync(Guid customerId, CancellationToken cancellationToken) =>
        context.Follows.CountAsync(f => f.FollowedId == customerId, cancellationToken);

    public void AddFollow(Follow follow) => context.Follows.Add(follow);

    public void RemoveFollow(Follow follow) => context.Follows.Remove(follow);
}
