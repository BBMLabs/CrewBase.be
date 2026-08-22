using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Community;
using RowingClub.Scheduling.Domain.Customers;

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
    DateTimeOffset CreatedAtUtc);

public sealed record CommentDto(Guid Id, string AuthorName, string Body, DateTimeOffset CreatedAtUtc);

public sealed record ParticipantDto(string FullName, int Level, DateTimeOffset JoinedAtUtc);

/// <summary>Kulüp akışı. ViewerCustomerId=null → firma paneli görünümü.</summary>
public sealed record GetFeedQuery(Guid? ViewerCustomerId, int Take) : IRequest<List<PostDto>>;

public sealed record GetPostMediaQuery(Guid PostId) : IRequest<PostMediaDto?>;

public sealed record PostMediaDto(string Base64, string ContentType);

/// <summary>AuthorCustomerId=null → kulüp (firma) adına paylaşım.</summary>
public sealed record CreatePostCommand(
    Guid? AuthorCustomerId, string Body, string? MediaBase64, string? MediaContentType,
    bool IsEvent, string? EventTitle, string? EventDate) : ICommand<Guid>;

/// <summary>Firma her paylaşımı, üye yalnızca kendi paylaşımını silebilir.</summary>
public sealed record DeletePostCommand(Guid PostId, Guid? RequesterCustomerId, bool IsCompany) : ICommand<Unit>;

public sealed record ToggleLikeCommand(Guid PostId, Guid CustomerId) : ICommand<ToggleResult>;

public sealed record ToggleResult(bool Active, int Count);

public sealed record GetCommentsQuery(Guid PostId) : IRequest<List<CommentDto>>;

public sealed record AddCommentCommand(Guid PostId, Guid CustomerId, string Body) : ICommand<CommentDto>;

public sealed record ToggleParticipationCommand(Guid PostId, Guid CustomerId) : ICommand<ToggleResult>;

public sealed record GetParticipantsQuery(Guid PostId) : IRequest<List<ParticipantDto>>;

public sealed record ToggleFollowCommand(Guid FollowerId, Guid TargetCustomerId) : ICommand<ToggleResult>;

public sealed class GetFeedQueryHandler(
    ICommunityRepository repository,
    ICustomerRepository customerRepository)
    : IRequestHandler<GetFeedQuery, List<PostDto>>
{
    public async Task<List<PostDto>> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        var posts = await repository.GetFeedAsync(Math.Clamp(request.Take, 1, 100), cancellationToken);
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

        var authors = (await customerRepository.GetAllAsync(cancellationToken))
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
            p.CreatedAtUtc)).ToList();
    }
}

public sealed class GetPostMediaQueryHandler(ICommunityRepository repository)
    : IRequestHandler<GetPostMediaQuery, PostMediaDto?>
{
    public async Task<PostMediaDto?> Handle(GetPostMediaQuery request, CancellationToken cancellationToken)
    {
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

        var post = Post.Create(
            request.AuthorCustomerId, request.Body, mediaKind, mediaContentType,
            request.IsEvent, request.EventTitle, eventDate);

        repository.AddPost(post,
            request.MediaBase64 is null ? null : PostMedia.Create(post.Id, request.MediaBase64, mediaContentType!));

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
        var comments = await repository.GetCommentsAsync(request.PostId, cancellationToken);
        var names = (await customerRepository.GetAllAsync(cancellationToken))
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
        var participants = await repository.GetParticipantsAsync(request.PostId, cancellationToken);
        var customers = (await customerRepository.GetAllAsync(cancellationToken))
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
