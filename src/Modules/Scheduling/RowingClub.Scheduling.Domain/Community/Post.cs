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
    public const int MaxMediaItems = 10;

    public Guid Id { get; private set; }

    /// <summary>null = kulüp (firma) paylaşımı; dolu = paylaşan üye.</summary>
    public Guid? AuthorCustomerId { get; private set; }

    public string Body { get; private set; } = null!;

    public PostMediaKind MediaKind { get; private set; }

    public string? MediaContentType { get; private set; }

    public bool IsEvent { get; private set; }

    public string? EventTitle { get; private set; }

    public DateOnly? EventDate { get; private set; }

    public bool IsPoll { get; private set; }

    public DateOnly? PollClosesOn { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Post()
    {
    }

    public static Post Create(
        Guid? authorCustomerId, string body,
        PostMediaKind mediaKind, string? mediaContentType,
        bool isEvent, string? eventTitle, DateOnly? eventDate,
        bool isPoll = false, DateOnly? pollClosesOn = null)
    {
        body = body?.Trim() ?? "";
        if (isPoll)
            EnsurePollAllowed(authorCustomerId, body, isEvent);
        if (body.Length == 0 && mediaKind == PostMediaKind.None)
            throw new DomainException("empty_post", "Paylaşım boş olamaz; yazı veya medya ekleyin.");
        if (body.Length > MaxBodyLength)
            throw new DomainException("post_too_long", $"Paylaşım en fazla {MaxBodyLength} karakter olabilir.");

        if (isEvent && authorCustomerId is not null)
            throw new DomainException("event_club_only", "Etkinlik yalnızca kulüp tarafından oluşturulabilir.");
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
            IsPoll = isPoll,
            PollClosesOn = isPoll ? pollClosesOn : null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public bool IsPollClosed(DateOnly today) => PollClosesOn is { } closesOn && today > closesOn;

    public void EnsureAcceptsVote(Guid optionId, IEnumerable<PollOption> options, DateOnly today)
    {
        if (!IsPoll)
            throw new DomainException("not_a_poll", "Bu paylaşım bir anket değil.");
        if (IsPollClosed(today))
            throw new DomainException("poll_closed", "Bu anket oylamaya kapandı.");
        if (!options.Any(o => o.Id == optionId && o.PostId == Id))
            throw new DomainException("poll_option_invalid", "Seçilen seçenek bu ankete ait değil.");
    }

    public void ClosePoll(DateOnly today)
    {
        if (!IsPoll)
            throw new DomainException("not_a_poll", "Bu paylaşım bir anket değil.");

        var yesterday = today.AddDays(-1);
        if (PollClosesOn is null || PollClosesOn > yesterday)
            PollClosesOn = yesterday;
    }

    private static void EnsurePollAllowed(Guid? authorCustomerId, string body, bool isEvent)
    {
        if (authorCustomerId is not null)
            throw new DomainException("poll_club_only", "Anket yalnızca kulüp tarafından oluşturulabilir.");
        if (isEvent)
            throw new DomainException("poll_event_conflict", "Bir paylaşım aynı anda hem etkinlik hem anket olamaz.");
        if (body.Length == 0)
            throw new DomainException("poll_question_required", "Anket sorusu zorunludur.");
    }
}

public sealed class PollOption
{
    public const int MaxTextLength = 80;
    public const int MinOptions = 2;
    public const int MaxOptions = 6;

    public Guid Id { get; private set; }

    public Guid PostId { get; private set; }

    public string Text { get; private set; } = null!;

    public int Order { get; private set; }

    private PollOption()
    {
    }

    public static List<PollOption> CreateSet(Guid postId, IEnumerable<string?> texts)
    {
        var normalized = (texts ?? []).Select(t => t?.Trim() ?? "").ToList();

        var valid = normalized.Count is >= MinOptions and <= MaxOptions
            && normalized.All(t => t.Length is > 0 and <= MaxTextLength)
            && normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() == normalized.Count;
        if (!valid)
            throw new DomainException(
                "poll_options_invalid",
                $"Anket {MinOptions}-{MaxOptions} farklı seçenek içermeli; her seçenek 1-{MaxTextLength} karakter olmalıdır.");

        return normalized
            .Select((text, index) => new PollOption { Id = Guid.NewGuid(), PostId = postId, Text = text, Order = index })
            .ToList();
    }
}

public sealed class PollVote
{
    public Guid PostId { get; private set; }

    public Guid OptionId { get; private set; }

    public Guid CustomerId { get; private set; }

    public DateTimeOffset AtUtc { get; private set; }

    private PollVote()
    {
    }

    public static PollVote Create(Guid postId, Guid optionId, Guid customerId) =>
        new() { PostId = postId, OptionId = optionId, CustomerId = customerId, AtUtc = DateTimeOffset.UtcNow };

    public void ChangeOption(Guid optionId)
    {
        OptionId = optionId;
        AtUtc = DateTimeOffset.UtcNow;
    }
}

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

    public static List<(PostMediaKind Kind, string ContentType)> ValidateSet(
        IReadOnlyList<(string Base64, string? ContentType)> items)
    {
        if (items.Count > Post.MaxMediaItems)
            throw new DomainValidationException(
                "media_too_many", $"Bir paylaşıma en fazla {Post.MaxMediaItems} görsel eklenebilir.");

        var validated = items.Select(i => Validate(i.Base64, i.ContentType)).ToList();
        if (validated.Count > 1 && validated.Any(v => v.Kind == PostMediaKind.Video))
            throw new DomainValidationException(
                "media_invalid_combination", "Video paylaşımında başka medya bulunamaz; video tek başına paylaşılmalıdır.");

        return validated;
    }
}

public sealed class PostMedia
{
    public Guid PostId { get; private set; }

    public int Order { get; private set; }

    public string Base64 { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    private PostMedia()
    {
    }

    public static PostMedia Create(Guid postId, string base64, string contentType, int order = 0) =>
        new() { PostId = postId, Order = order, Base64 = base64, ContentType = contentType };
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

public sealed class PostReaction
{
    public const string ThumbsUp = "👍";
    public const int MaxEmojiLength = 16;

    public static readonly IReadOnlyList<string> AllowedEmojis = ["👍", "❤️", "😂", "🎉", "😮", "👏"];

    public Guid Id { get; private set; }

    public Guid PostId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public string Emoji { get; private set; } = null!;

    public DateTimeOffset AtUtc { get; private set; }

    public bool IsClub => CustomerId is null;

    private PostReaction()
    {
    }

    public static string NormalizeEmoji(string? emoji)
    {
        var value = emoji?.Trim() ?? "";
        if (value == "❤")
            value = "❤️";
        if (!AllowedEmojis.Contains(value))
            throw new DomainValidationException(
                "invalid_reaction", "Geçersiz tepki; yalnızca izin verilen emojiler kullanılabilir.");
        return value;
    }

    public static PostReaction Create(Guid postId, Guid? customerId, string emoji) =>
        new()
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            CustomerId = customerId,
            Emoji = NormalizeEmoji(emoji),
            AtUtc = DateTimeOffset.UtcNow,
        };

    public static ReactionChange Apply(PostReaction? existing, Guid postId, Guid? customerId, string? requestedEmoji)
    {
        if (requestedEmoji is null)
            return existing is null
                ? new ReactionChange(ReactionChangeKind.None, null)
                : new ReactionChange(ReactionChangeKind.Removed, existing);

        var emoji = NormalizeEmoji(requestedEmoji);
        if (existing is null)
            return new ReactionChange(ReactionChangeKind.Added, Create(postId, customerId, emoji));
        if (existing.Emoji == emoji)
            return new ReactionChange(ReactionChangeKind.Removed, existing);

        existing.Emoji = emoji;
        existing.AtUtc = DateTimeOffset.UtcNow;
        return new ReactionChange(ReactionChangeKind.Changed, existing);
    }
}

public enum ReactionChangeKind
{
    None = 0,
    Added = 1,
    Changed = 2,
    Removed = 3,
}

public sealed record ReactionChange(ReactionChangeKind Kind, PostReaction? Reaction)
{
    public string? CurrentEmoji =>
        Kind is ReactionChangeKind.Added or ReactionChangeKind.Changed ? Reaction!.Emoji : null;
}

public sealed class PostComment
{
    public const int MaxLength = 500;

    public Guid Id { get; private set; }

    public Guid PostId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public Guid? ParentCommentId { get; private set; }

    public string Body { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsClub => CustomerId is null;

    private PostComment()
    {
    }

    public static PostComment Create(Guid postId, Guid? customerId, string body, PostComment? parent = null)
    {
        body = body?.Trim() ?? "";
        if (body.Length is 0 or > MaxLength)
            throw new DomainException("invalid_comment", $"Yorum 1-{MaxLength} karakter olmalıdır.");

        if (parent is not null && (parent.PostId != postId || parent.ParentCommentId is not null))
            throw new DomainValidationException(
                "invalid_parent", "Yanıt yalnızca aynı paylaşımdaki bir ana yoruma verilebilir.");

        return new PostComment
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            CustomerId = customerId,
            ParentCommentId = parent?.Id,
            Body = body,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void EnsureCanBeDeletedBy(Guid? actorCustomerId)
    {
        if (actorCustomerId is not null && CustomerId != actorCustomerId)
            throw new DomainException("forbidden", "Yalnızca kendi yorumunuzu silebilirsiniz.");
    }
}

public sealed class CommentLike
{
    public Guid Id { get; private set; }

    public Guid CommentId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public DateTimeOffset AtUtc { get; private set; }

    private CommentLike()
    {
    }

    public static CommentLike Create(Guid commentId, Guid? customerId) =>
        new() { Id = Guid.NewGuid(), CommentId = commentId, CustomerId = customerId, AtUtc = DateTimeOffset.UtcNow };
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
