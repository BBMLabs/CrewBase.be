namespace RowingClub.Scheduling.Domain.Social;

/// <summary>
/// İki arkadaş üye arasındaki mesaj. İçerik veritabanında şifreli saklanır; gerçek zamanlı
/// iletim SignalR ile API katmanında yapılır, burada yalnızca kalıcı kayıt tutulur.
/// </summary>
public sealed class DirectMessage
{
    public const int MaxLength = 2000;

    public Guid Id { get; private set; }

    public Guid SenderId { get; private set; }

    public Guid RecipientId { get; private set; }

    public string Body { get; private set; } = null!;

    public DateTimeOffset SentAtUtc { get; private set; }

    public DateTimeOffset? ReadAtUtc { get; private set; }

    private DirectMessage()
    {
    }

    public static DirectMessage Send(Guid senderId, Guid recipientId, string body) => new()
    {
        Id = Guid.NewGuid(),
        SenderId = senderId,
        RecipientId = recipientId,
        Body = body.Trim(),
        SentAtUtc = DateTimeOffset.UtcNow,
    };

    public void MarkRead()
    {
        ReadAtUtc ??= DateTimeOffset.UtcNow;
    }
}
