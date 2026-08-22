using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Community;

public enum PostMediaKind
{
    None = 0,
    Image = 1,
    Video = 2,
}

/// <summary>
/// Kulüp akışındaki paylaşım: yazı, görsel veya kısa video (15-20 sn); istenirse ETKİNLİK olarak
/// işaretlenir ve üyeler katılım bildirir. Firma (kulüp) adına ya da üye adına paylaşılabilir.
/// Medya süresi istemcide doğrulanır (sunucu tarafında boyut sınırı uygulanır); içerik ve medya
/// veritabanında şifreli saklanır.
/// </summary>
public sealed class Post
{
    public const int MaxBodyLength = 2000;
    public const int MaxImageBytes = 5 * 1024 * 1024;
    public const int MaxVideoBytes = 25 * 1024 * 1024;

    public Guid Id { get; private set; }

    /// <summary>null = kulüp (firma) paylaşımı; dolu = paylaşan üye.</summary>
    public Guid? AuthorCustomerId { get; private set; }

    public string Body { get; private set; } = null!;

    public PostMediaKind MediaKind { get; private set; }

    public string? MediaContentType { get; private set; }

    public bool IsEvent { get; private set; }

    public string? EventTitle { get; private set; }

    public DateOnly? EventDate { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Post()
    {
    }

    public static Post Create(
        Guid? authorCustomerId, string body,
        PostMediaKind mediaKind, string? mediaContentType,
        bool isEvent, string? eventTitle, DateOnly? eventDate)
    {
        body = body?.Trim() ?? "";
        if (body.Length == 0 && mediaKind == PostMediaKind.None)
            throw new DomainException("empty_post", "Paylaşım boş olamaz; yazı veya medya ekleyin.");
        if (body.Length > MaxBodyLength)
            throw new DomainException("post_too_long", $"Paylaşım en fazla {MaxBodyLength} karakter olabilir.");

        if (isEvent && string.IsNullOrWhiteSpace(eventTitle))
            throw new DomainException("event_title_required", "Etkinlik başlığı zorunludur.");

        return new Post
        {
            Id = Guid.NewGuid(),
            AuthorCustomerId = authorCustomerId,
            Body = body,
            MediaKind = mediaKind,
            MediaContentType = mediaKind == PostMediaKind.None ? null : mediaContentType!.ToLowerInvariant(),
            IsEvent = isEvent,
            EventTitle = isEvent ? eventTitle!.Trim() : null,
            EventDate = isEvent ? eventDate : null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>Medya doğrulama: tür + boyut. Video süresi (15-20 sn) istemcide denetlenir.</summary>
public static class PostMediaValidator
{
    public static (PostMediaKind Kind, string ContentType) Validate(string base64, string? contentTypeRaw)
    {
        var contentType = (contentTypeRaw ?? "").ToLowerInvariant();
        var estimatedBytes = base64.Length * 3 / 4;

        if (contentType is "image/jpeg" or "image/png" or "image/gif" or "image/webp")
        {
            if (estimatedBytes > Post.MaxImageBytes)
                throw new DomainException("media_too_large", "Görsel en fazla 5MB olabilir.");
            return (PostMediaKind.Image, contentType);
        }

        if (contentType is "video/mp4" or "video/webm" or "video/quicktime")
        {
            if (estimatedBytes > Post.MaxVideoBytes)
                throw new DomainException("media_too_large", "Video en fazla 25MB olabilir (15-20 saniye).");
            return (PostMediaKind.Video, contentType);
        }

        throw new DomainException("media_invalid_type", "Medya görsel (JPG/PNG/GIF/WebP) veya video (MP4/WebM) olmalıdır.");
    }
}

/// <summary>Paylaşım medyası - akış sorgusu ağır medyaya dokunmasın diye ayrı tabloda.</summary>
public sealed class PostMedia
{
    public Guid PostId { get; private set; }

    public string Base64 { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    private PostMedia()
    {
    }

    public static PostMedia Create(Guid postId, string base64, string contentType) =>
        new() { PostId = postId, Base64 = base64, ContentType = contentType };
}

public sealed class PostLike
{
    public Guid PostId { get; private set; }

    public Guid CustomerId { get; private set; }

    public DateTimeOffset AtUtc { get; private set; }

    private PostLike()
    {
    }

    public static PostLike Create(Guid postId, Guid customerId) =>
        new() { PostId = postId, CustomerId = customerId, AtUtc = DateTimeOffset.UtcNow };
}

public sealed class PostComment
{
    public const int MaxLength = 500;

    public Guid Id { get; private set; }

    public Guid PostId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Body { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private PostComment()
    {
    }

    public static PostComment Create(Guid postId, Guid customerId, string body)
    {
        body = body?.Trim() ?? "";
        if (body.Length is 0 or > MaxLength)
            throw new DomainException("invalid_comment", $"Yorum 1-{MaxLength} karakter olmalıdır.");

        return new PostComment
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            CustomerId = customerId,
            Body = body,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>Etkinlik katılımı ("Katılıyorum").</summary>
public sealed class EventParticipation
{
    public Guid PostId { get; private set; }

    public Guid CustomerId { get; private set; }

    public DateTimeOffset JoinedAtUtc { get; private set; }

    private EventParticipation()
    {
    }

    public static EventParticipation Create(Guid postId, Guid customerId) =>
        new() { PostId = postId, CustomerId = customerId, JoinedAtUtc = DateTimeOffset.UtcNow };
}

/// <summary>Tek yönlü takip (arkadaşlıktan bağımsız).</summary>
public sealed class Follow
{
    public Guid FollowerId { get; private set; }

    public Guid FollowedId { get; private set; }

    public DateTimeOffset AtUtc { get; private set; }

    private Follow()
    {
    }

    public static Follow Create(Guid followerId, Guid followedId)
    {
        if (followerId == followedId)
            throw new DomainException("cannot_follow_self", "Kendinizi takip edemezsiniz.");

        return new Follow { FollowerId = followerId, FollowedId = followedId, AtUtc = DateTimeOffset.UtcNow };
    }
}
