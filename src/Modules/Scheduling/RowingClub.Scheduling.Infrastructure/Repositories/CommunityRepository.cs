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

    public Task<PostMedia?> GetMediaAsync(Guid postId, int order, CancellationToken cancellationToken) =>
        context.Set<PostMedia>().FirstOrDefaultAsync(m => m.PostId == postId && m.Order == order, cancellationToken);

    public async Task<Dictionary<Guid, int>> GetMediaCountsAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken) =>
        (await context.Set<PostMedia>().Where(m => postIds.Contains(m.PostId))
            .GroupBy(m => m.PostId).Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.Key, x => x.Count);

    public void AddPost(Post post, IReadOnlyCollection<PostMedia> media)
    {
        context.Posts.Add(post);
        context.Set<PostMedia>().AddRange(media);
    }

    public void RemovePost(Post post) => context.Posts.Remove(post);

    public async Task<List<ReactionCount>> GetReactionCountsAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken) =>
        (await context.PostReactions.Where(r => postIds.Contains(r.PostId))
            .GroupBy(r => new { r.PostId, r.Emoji })
            .Select(g => new { g.Key.PostId, g.Key.Emoji, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .Select(x => new ReactionCount(x.PostId, x.Emoji, x.Count)).ToList();

    public async Task<Dictionary<Guid, string>> GetMyReactionsAsync(
        Guid? customerId, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken) =>
        (await context.PostReactions
            .Where(r => r.CustomerId == customerId && postIds.Contains(r.PostId))
            .Select(r => new { r.PostId, r.Emoji }).ToListAsync(cancellationToken))
            .ToDictionary(x => x.PostId, x => x.Emoji);

    public Task<List<PostReaction>> GetReactionsAsync(Guid postId, CancellationToken cancellationToken) =>
        context.PostReactions.Where(r => r.PostId == postId).ToListAsync(cancellationToken);

    public Task<PostReaction?> GetReactionAsync(Guid postId, Guid? customerId, CancellationToken cancellationToken) =>
        context.PostReactions.FirstOrDefaultAsync(
            r => r.PostId == postId && r.CustomerId == customerId, cancellationToken);

    public void AddReaction(PostReaction reaction) => context.PostReactions.Add(reaction);

    public void RemoveReaction(PostReaction reaction) => context.PostReactions.Remove(reaction);

    public async Task<Dictionary<Guid, int>> GetCommentCountsAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken) =>
        (await context.PostComments.Where(c => postIds.Contains(c.PostId))
            .GroupBy(c => c.PostId).Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.Key, x => x.Count);

    public Task<List<PostComment>> GetCommentsAsync(Guid postId, CancellationToken cancellationToken) =>
        context.PostComments.Where(c => c.PostId == postId).ToListAsync(cancellationToken);

    public Task<PostComment?> GetCommentAsync(Guid commentId, CancellationToken cancellationToken) =>
        context.PostComments.FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

    public Task<List<PostComment>> GetRepliesAsync(Guid parentCommentId, CancellationToken cancellationToken) =>
        context.PostComments.Where(c => c.ParentCommentId == parentCommentId).ToListAsync(cancellationToken);

    public void AddComment(PostComment comment) => context.PostComments.Add(comment);

    public void RemoveComment(PostComment comment) => context.PostComments.Remove(comment);

    public async Task<Dictionary<Guid, int>> GetCommentLikeCountsAsync(
        IReadOnlyCollection<Guid> commentIds, CancellationToken cancellationToken) =>
        (await context.CommentLikes.Where(l => commentIds.Contains(l.CommentId))
            .GroupBy(l => l.CommentId).Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.Key, x => x.Count);

    public async Task<HashSet<Guid>> GetLikedCommentIdsAsync(
        Guid? customerId, IReadOnlyCollection<Guid> commentIds, CancellationToken cancellationToken) =>
        (await context.CommentLikes
            .Where(l => l.CustomerId == customerId && commentIds.Contains(l.CommentId))
            .Select(l => l.CommentId).ToListAsync(cancellationToken)).ToHashSet();

    public Task<CommentLike?> GetCommentLikeAsync(Guid commentId, Guid? customerId, CancellationToken cancellationToken) =>
        context.CommentLikes.FirstOrDefaultAsync(
            l => l.CommentId == commentId && l.CustomerId == customerId, cancellationToken);

    public void AddCommentLike(CommentLike like) => context.CommentLikes.Add(like);

    public void RemoveCommentLike(CommentLike like) => context.CommentLikes.Remove(like);

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

    public void AddPollOptions(IEnumerable<PollOption> options) => context.PollOptions.AddRange(options);

    public Task<List<PollOption>> GetPollOptionsAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken) =>
        context.PollOptions.Where(o => postIds.Contains(o.PostId))
            .OrderBy(o => o.PostId).ThenBy(o => o.Order).ToListAsync(cancellationToken);

    public async Task<Dictionary<Guid, int>> GetPollVoteCountsByOptionAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken) =>
        (await context.PollVotes.Where(v => postIds.Contains(v.PostId))
            .GroupBy(v => v.OptionId).Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.Key, x => x.Count);

    public async Task<Dictionary<Guid, Guid>> GetVotedOptionIdsAsync(
        Guid customerId, IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken) =>
        (await context.PollVotes
            .Where(v => v.CustomerId == customerId && postIds.Contains(v.PostId))
            .Select(v => new { v.PostId, v.OptionId }).ToListAsync(cancellationToken))
            .ToDictionary(x => x.PostId, x => x.OptionId);

    public Task<List<PollVote>> GetPollVotesAsync(Guid postId, CancellationToken cancellationToken) =>
        context.PollVotes.Where(v => v.PostId == postId).ToListAsync(cancellationToken);

    public Task<PollVote?> GetPollVoteAsync(Guid postId, Guid customerId, CancellationToken cancellationToken) =>
        context.PollVotes.FirstOrDefaultAsync(
            v => v.PostId == postId && v.CustomerId == customerId, cancellationToken);

    public void AddPollVote(PollVote vote) => context.PollVotes.Add(vote);

    public void RemovePollVote(PollVote vote) => context.PollVotes.Remove(vote);

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
