using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Community;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Application.Community;

public sealed record PostDto(
    Guid Id,
    string AuthorName,
    Guid? AuthorCustomerId,
    bool IsClubPost,
    string Body,
    string MediaKind,
    string? MediaContentType,
    bool IsEvent,
    string? EventTitle,
    string? EventDate,
    int LikeCount,
    bool LikedByMe,
    int CommentCount,
    int ParticipantCount,
    bool JoinedByMe,
    bool FollowingAuthor,
    bool IsMine,
    DateTimeOffset CreatedAtUtc,
    bool IsPoll,
    PollDto? Poll);

public sealed record PollOptionDto(Guid Id, string Text, int VoteCount, bool VotedByMe);

public sealed record PollDto(
    List<PollOptionDto> Options, int TotalVotes, string? ClosesOn, bool Closed, Guid? MyOptionId);

public sealed record PollVoterDto(Guid OptionId, string FullName, int Level, DateTimeOffset VotedAtUtc);

public sealed record CommentDto(Guid Id, string AuthorName, string Body, DateTimeOffset CreatedAtUtc);

public sealed record ParticipantDto(string FullName, int Level, DateTimeOffset JoinedAtUtc);

public sealed record LikerDto(string FullName, int Level, DateTimeOffset LikedAtUtc);

/// <summary>Kulüp akışı. ViewerCustomerId=null → firma paneli görünümü.</summary>
public sealed record GetFeedQuery(Guid? ViewerCustomerId, int Take, bool ClubOnly) : IRequest<List<PostDto>>;

public sealed record GetPostMediaQuery(Guid PostId, bool ClubOnly) : IRequest<PostMediaDto?>;

public sealed record PostMediaDto(string Base64, string ContentType);

/// <summary>AuthorCustomerId=null → kulüp (firma) adına paylaşım.</summary>
public sealed record CreatePostCommand(
    Guid? AuthorCustomerId, string Body, string? MediaBase64, string? MediaContentType,
    bool IsEvent, string? EventTitle, string? EventDate,
    IReadOnlyList<string>? PollOptions = null, string? PollClosesOn = null) : ICommand<Guid>;

/// <summary>Firma her paylaşımı, üye yalnızca kendi paylaşımını silebilir.</summary>
public sealed record DeletePostCommand(Guid PostId, Guid? RequesterCustomerId, bool IsCompany) : ICommand<Unit>;

public sealed record ToggleLikeCommand(Guid PostId, Guid CustomerId) : ICommand<ToggleResult>;

public sealed record ToggleResult(bool Active, int Count);

public sealed record GetCommentsQuery(Guid PostId, bool ClubOnly) : IRequest<List<CommentDto>>;

public sealed record AddCommentCommand(Guid PostId, Guid CustomerId, string Body) : ICommand<CommentDto>;

public sealed record ToggleParticipationCommand(Guid PostId, Guid CustomerId) : ICommand<ToggleResult>;

public sealed record GetParticipantsQuery(Guid PostId, bool ClubOnly) : IRequest<List<ParticipantDto>>;

public sealed record GetLikersQuery(Guid PostId) : IRequest<List<LikerDto>>;

public sealed record CastPollVoteCommand(Guid PostId, Guid CustomerId, Guid OptionId) : ICommand<PollDto>;

public sealed record GetPollVotersQuery(Guid PostId) : IRequest<List<PollVoterDto>>;

public sealed record ClosePollCommand(Guid PostId) : ICommand<Unit>;

public sealed record ToggleFollowCommand(Guid FollowerId, Guid TargetCustomerId) : ICommand<ToggleResult>;

public sealed class GetFeedQueryHandler(
    ICommunityRepository repository,
    ICustomerRepository customerRepository,
    ISettingsRepository settingsRepository)
    : IRequestHandler<GetFeedQuery, List<PostDto>>
{
    public async Task<List<PostDto>> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        var posts = await repository.GetFeedAsync(Math.Clamp(request.Take, 1, 100), request.ClubOnly, cancellationToken);
        var postIds = posts.Select(p => p.Id).ToList();

        var likeCounts = await repository.GetLikeCountsAsync(postIds, cancellationToken);
        var commentCounts = await repository.GetCommentCountsAsync(postIds, cancellationToken);
        var participantCounts = await repository.GetParticipantCountsAsync(postIds, cancellationToken);

        var likedByMe = new HashSet<Guid>();
        var joinedByMe = new HashSet<Guid>();
        var following = new HashSet<Guid>();
        if (request.ViewerCustomerId is { } viewer)
        {
            likedByMe = await repository.GetLikedPostIdsAsync(viewer, postIds, cancellationToken);
            joinedByMe = await repository.GetJoinedPostIdsAsync(viewer, postIds, cancellationToken);
            following = await repository.GetFollowedIdsAsync(viewer, cancellationToken);
        }

        var polls = await PollReader.LoadAsync(
            repository, settingsRepository, posts, request.ViewerCustomerId, cancellationToken);

        var authorIds = posts.Where(p => p.AuthorCustomerId is not null).Select(p => p.AuthorCustomerId!.Value).Distinct().ToList();
        var authors = (await customerRepository.GetByIdsAsync(authorIds, cancellationToken))
            .ToDictionary(c => c.Id, c => c.FullName);

        return posts.Select(p => new PostDto(
            p.Id,
            p.AuthorCustomerId is { } a ? authors.GetValueOrDefault(a, "(silinmiş üye)") : "Kulüp",
            p.AuthorCustomerId,
            p.AuthorCustomerId is null,
            p.Body,
            p.MediaKind.ToString(),
            p.MediaContentType,
            p.IsEvent,
            p.EventTitle,
            p.EventDate?.ToString("yyyy-MM-dd"),
            likeCounts.GetValueOrDefault(p.Id),
            likedByMe.Contains(p.Id),
            commentCounts.GetValueOrDefault(p.Id),
            participantCounts.GetValueOrDefault(p.Id),
            joinedByMe.Contains(p.Id),
            p.AuthorCustomerId is { } author && following.Contains(author),
            p.AuthorCustomerId == request.ViewerCustomerId && p.AuthorCustomerId is not null,
            p.CreatedAtUtc,
            p.IsPoll,
            polls.GetValueOrDefault(p.Id))).ToList();
    }
}

internal static class PollReader
{
    public static async Task<Dictionary<Guid, PollDto>> LoadAsync(
        ICommunityRepository repository,
        ISettingsRepository settingsRepository,
        IReadOnlyCollection<Post> posts,
        Guid? viewerCustomerId,
        CancellationToken cancellationToken)
    {
        var pollPosts = posts.Where(p => p.IsPoll).ToList();
        if (pollPosts.Count == 0)
            return [];

        var pollIds = pollPosts.Select(p => p.Id).ToList();
        var options = await repository.GetPollOptionsAsync(pollIds, cancellationToken);
        var voteCounts = await repository.GetPollVoteCountsByOptionAsync(pollIds, cancellationToken);
        var myVotes = viewerCustomerId is { } viewer
            ? await repository.GetVotedOptionIdsAsync(viewer, pollIds, cancellationToken)
            : [];
        var today = await TodayAsync(settingsRepository, cancellationToken);

        var optionsByPost = options.ToLookup(o => o.PostId);
        return pollPosts.ToDictionary(
            p => p.Id,
            p => Build(
                p, optionsByPost[p.Id], voteCounts,
                myVotes.TryGetValue(p.Id, out var mine) ? mine : null, today));
    }

    public static async Task<DateOnly> TodayAsync(
        ISettingsRepository settingsRepository, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken) ?? CompanySettings.Default();
        return DateOnly.FromDateTime(settings.NowLocal());
    }

    public static PollDto Build(
        Post post, IEnumerable<PollOption> options, IReadOnlyDictionary<Guid, int> voteCounts,
        Guid? myOptionId, DateOnly today)
    {
        var optionDtos = options
            .OrderBy(o => o.Order)
            .Select(o => new PollOptionDto(o.Id, o.Text, voteCounts.GetValueOrDefault(o.Id), myOptionId == o.Id))
            .ToList();

        return new PollDto(
            optionDtos,
            optionDtos.Sum(o => o.VoteCount),
            post.PollClosesOn?.ToString("yyyy-MM-dd"),
            post.IsPollClosed(today),
            myOptionId);
    }
}

internal static class CommunityGuards
{
    public static async Task<bool> IsClubPostAsync(
        ICommunityRepository repository, Guid postId, CancellationToken cancellationToken)
    {
        var post = await repository.GetPostAsync(postId, cancellationToken);
        return post is not null && post.AuthorCustomerId is null;
    }
}

public sealed class GetPostMediaQueryHandler(ICommunityRepository repository)
    : IRequestHandler<GetPostMediaQuery, PostMediaDto?>
{
    public async Task<PostMediaDto?> Handle(GetPostMediaQuery request, CancellationToken cancellationToken)
    {
        if (request.ClubOnly && !await CommunityGuards.IsClubPostAsync(repository, request.PostId, cancellationToken))
            return null;

        var media = await repository.GetMediaAsync(request.PostId, cancellationToken);
        return media is null ? null : new PostMediaDto(media.Base64, media.ContentType);
    }
}

public sealed class CreatePostCommandHandler(
    ICommunityRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        DateOnly? eventDate = null;
        if (!string.IsNullOrWhiteSpace(request.EventDate))
        {
            if (!DateOnly.TryParseExact(request.EventDate, "yyyy-MM-dd", out var parsed))
                throw new DomainException("invalid_date", "Etkinlik tarihi YYYY-AA-GG biçiminde olmalıdır.");
            eventDate = parsed;
        }

        var mediaKind = PostMediaKind.None;
        string? mediaContentType = null;
        if (request.MediaBase64 is not null)
            (mediaKind, mediaContentType) = PostMediaValidator.Validate(request.MediaBase64, request.MediaContentType);

        var isPoll = request.PollOptions is { Count: > 0 };
        DateOnly? pollClosesOn = null;
        if (isPoll && !string.IsNullOrWhiteSpace(request.PollClosesOn))
        {
            if (!DateOnly.TryParseExact(request.PollClosesOn, "yyyy-MM-dd", out var parsedClose))
                throw new DomainException("invalid_date", "Anket bitiş tarihi YYYY-AA-GG biçiminde olmalıdır.");
            pollClosesOn = parsedClose;
        }

        var post = Post.Create(
            request.AuthorCustomerId, request.Body, mediaKind, mediaContentType,
            request.IsEvent, request.EventTitle, eventDate, isPoll, pollClosesOn);
        var pollOptions = isPoll ? PollOption.CreateSet(post.Id, request.PollOptions!) : [];

        repository.AddPost(post,
            request.MediaBase64 is null ? null : PostMedia.Create(post.Id, request.MediaBase64, mediaContentType!));
        if (pollOptions.Count > 0)
            repository.AddPollOptions(pollOptions);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return post.Id;
    }
}

public sealed class DeletePostCommandHandler(
    ICommunityRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<DeletePostCommand, Unit>
{
    public async Task<Unit> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await repository.GetPostAsync(request.PostId, cancellationToken)
            ?? throw new NotFoundException("Post", request.PostId.ToString());

        var allowed = request.IsCompany || post.AuthorCustomerId == request.RequesterCustomerId;
        if (!allowed)
            throw new DomainException("forbidden", "Yalnızca kendi paylaşımınızı silebilirsiniz.");

        repository.RemovePost(post);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class ToggleLikeCommandHandler(
    ICommunityRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<ToggleLikeCommand, ToggleResult>
{
    public async Task<ToggleResult> Handle(ToggleLikeCommand request, CancellationToken cancellationToken)
    {
        _ = await repository.GetPostAsync(request.PostId, cancellationToken)
            ?? throw new NotFoundException("Post", request.PostId.ToString());

        var existing = await repository.GetLikeAsync(request.PostId, request.CustomerId, cancellationToken);
        var active = existing is null;
        if (existing is null)
            repository.AddLike(PostLike.Create(request.PostId, request.CustomerId));
        else
            repository.RemoveLike(existing);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var counts = await repository.GetLikeCountsAsync([request.PostId], cancellationToken);
        return new ToggleResult(active, counts.GetValueOrDefault(request.PostId));
    }
}

public sealed class GetCommentsQueryHandler(
    ICommunityRepository repository, ICustomerRepository customerRepository)
    : IRequestHandler<GetCommentsQuery, List<CommentDto>>
{
    public async Task<List<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        if (request.ClubOnly && !await CommunityGuards.IsClubPostAsync(repository, request.PostId, cancellationToken))
            return [];

        var comments = await repository.GetCommentsAsync(request.PostId, cancellationToken);
        var commenterIds = comments.Select(c => c.CustomerId).Distinct().ToList();
        var names = (await customerRepository.GetByIdsAsync(commenterIds, cancellationToken))
            .ToDictionary(c => c.Id, c => c.FullName);

        return comments
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => new CommentDto(c.Id, names.GetValueOrDefault(c.CustomerId, "(silinmiş üye)"), c.Body, c.CreatedAtUtc))
            .ToList();
    }
}

public sealed class AddCommentCommandHandler(
    ICommunityRepository repository,
    ICustomerRepository customerRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<AddCommentCommand, CommentDto>
{
    public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        _ = await repository.GetPostAsync(request.PostId, cancellationToken)
            ?? throw new NotFoundException("Post", request.PostId.ToString());

        var comment = PostComment.Create(request.PostId, request.CustomerId, request.Body);
        repository.AddComment(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var author = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        return new CommentDto(comment.Id, author?.FullName ?? "-", comment.Body, comment.CreatedAtUtc);
    }
}

public sealed class ToggleParticipationCommandHandler(
    ICommunityRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<ToggleParticipationCommand, ToggleResult>
{
    public async Task<ToggleResult> Handle(ToggleParticipationCommand request, CancellationToken cancellationToken)
    {
        var post = await repository.GetPostAsync(request.PostId, cancellationToken)
            ?? throw new NotFoundException("Post", request.PostId.ToString());

        if (!post.IsEvent)
            throw new DomainException("not_event", "Bu paylaşım bir etkinlik değil.");

        var existing = await repository.GetParticipationAsync(request.PostId, request.CustomerId, cancellationToken);
        var active = existing is null;
        if (existing is null)
            repository.AddParticipation(EventParticipation.Create(request.PostId, request.CustomerId));
        else
            repository.RemoveParticipation(existing);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var counts = await repository.GetParticipantCountsAsync([request.PostId], cancellationToken);
        return new ToggleResult(active, counts.GetValueOrDefault(request.PostId));
    }
}

public sealed class GetParticipantsQueryHandler(
    ICommunityRepository repository, ICustomerRepository customerRepository)
    : IRequestHandler<GetParticipantsQuery, List<ParticipantDto>>
{
    public async Task<List<ParticipantDto>> Handle(GetParticipantsQuery request, CancellationToken cancellationToken)
    {
        if (request.ClubOnly && !await CommunityGuards.IsClubPostAsync(repository, request.PostId, cancellationToken))
            return [];

        var participants = await repository.GetParticipantsAsync(request.PostId, cancellationToken);
        var participantIds = participants.Select(p => p.CustomerId).Distinct().ToList();
        var customers = (await customerRepository.GetByIdsAsync(participantIds, cancellationToken))
            .ToDictionary(c => c.Id);

        return participants
            .OrderBy(p => p.JoinedAtUtc)
            .Select(p =>
            {
                customers.TryGetValue(p.CustomerId, out var c);
                return new ParticipantDto(c?.FullName ?? "(silinmiş üye)", c?.Level ?? 0, p.JoinedAtUtc);
            })
            .ToList();
    }
}

public sealed class GetLikersQueryHandler(
    ICommunityRepository repository, ICustomerRepository customerRepository)
    : IRequestHandler<GetLikersQuery, List<LikerDto>>
{
    public async Task<List<LikerDto>> Handle(GetLikersQuery request, CancellationToken cancellationToken)
    {
        var likes = await repository.GetLikesAsync(request.PostId, cancellationToken);
        var customerIds = likes.Select(l => l.CustomerId).Distinct().ToList();
        var customers = (await customerRepository.GetByIdsAsync(customerIds, cancellationToken))
            .ToDictionary(c => c.Id);

        return likes
            .OrderByDescending(l => l.AtUtc)
            .Select(l =>
            {
                customers.TryGetValue(l.CustomerId, out var c);
                return new LikerDto(c?.FullName ?? "(silinmiş üye)", c?.Level ?? 0, l.AtUtc);
            })
            .ToList();
    }
}

public sealed class CastPollVoteCommandHandler(
    ICommunityRepository repository,
    ISettingsRepository settingsRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<CastPollVoteCommand, PollDto>
{
    public async Task<PollDto> Handle(CastPollVoteCommand request, CancellationToken cancellationToken)
    {
        var post = await repository.GetPostAsync(request.PostId, cancellationToken)
            ?? throw new NotFoundException("Post", request.PostId.ToString());

        var today = await PollReader.TodayAsync(settingsRepository, cancellationToken);
        var options = await repository.GetPollOptionsAsync([post.Id], cancellationToken);
        post.EnsureAcceptsVote(request.OptionId, options, today);

        var existing = await repository.GetPollVoteAsync(post.Id, request.CustomerId, cancellationToken);
        Guid? myOptionId = request.OptionId;
        if (existing is null)
        {
            repository.AddPollVote(PollVote.Create(post.Id, request.OptionId, request.CustomerId));
        }
        else if (existing.OptionId == request.OptionId)
        {
            repository.RemovePollVote(existing);
            myOptionId = null;
        }
        else
        {
            existing.ChangeOption(request.OptionId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var voteCounts = await repository.GetPollVoteCountsByOptionAsync([post.Id], cancellationToken);
        return PollReader.Build(post, options, voteCounts, myOptionId, today);
    }
}

public sealed class GetPollVotersQueryHandler(
    ICommunityRepository repository, ICustomerRepository customerRepository)
    : IRequestHandler<GetPollVotersQuery, List<PollVoterDto>>
{
    public async Task<List<PollVoterDto>> Handle(GetPollVotersQuery request, CancellationToken cancellationToken)
    {
        var votes = await repository.GetPollVotesAsync(request.PostId, cancellationToken);
        var customerIds = votes.Select(v => v.CustomerId).Distinct().ToList();
        var customers = (await customerRepository.GetByIdsAsync(customerIds, cancellationToken))
            .ToDictionary(c => c.Id);

        return votes
            .OrderByDescending(v => v.AtUtc)
            .Select(v =>
            {
                customers.TryGetValue(v.CustomerId, out var c);
                return new PollVoterDto(v.OptionId, c?.FullName ?? "(silinmiş üye)", c?.Level ?? 0, v.AtUtc);
            })
            .ToList();
    }
}

public sealed class ClosePollCommandHandler(
    ICommunityRepository repository,
    ISettingsRepository settingsRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<ClosePollCommand, Unit>
{
    public async Task<Unit> Handle(ClosePollCommand request, CancellationToken cancellationToken)
    {
        var post = await repository.GetPostAsync(request.PostId, cancellationToken)
            ?? throw new NotFoundException("Post", request.PostId.ToString());

        post.ClosePoll(await PollReader.TodayAsync(settingsRepository, cancellationToken));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class ToggleFollowCommandHandler(
    ICommunityRepository repository,
    ICustomerRepository customerRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<ToggleFollowCommand, ToggleResult>
{
    public async Task<ToggleResult> Handle(ToggleFollowCommand request, CancellationToken cancellationToken)
    {
        _ = await customerRepository.GetByIdAsync(request.TargetCustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.TargetCustomerId.ToString());

        var existing = await repository.GetFollowAsync(request.FollowerId, request.TargetCustomerId, cancellationToken);
        var active = existing is null;
        if (existing is null)
            repository.AddFollow(Follow.Create(request.FollowerId, request.TargetCustomerId));
        else
            repository.RemoveFollow(existing);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var followers = await repository.GetFollowerCountAsync(request.TargetCustomerId, cancellationToken);
        return new ToggleResult(active, followers);
    }
}
