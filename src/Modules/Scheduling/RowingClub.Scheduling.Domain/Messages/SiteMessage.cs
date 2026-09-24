using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Messages;

public sealed class SiteMessage
{
    public Guid Id { get; private set; }

    public string FullName { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string? Phone { get; private set; }

    public string Body { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string? IpAddress { get; private set; }

    public string? ReplyText { get; private set; }

    public DateTimeOffset? RepliedAtUtc { get; private set; }

    private SiteMessage()
    {
    }

    public static SiteMessage Create(string fullName, string email, string? phone, string body, string? ipAddress) => new()
    {
        Id = Guid.NewGuid(),
        FullName = fullName.Trim(),
        Email = email.Trim().ToLowerInvariant(),
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
        Body = body.Trim(),
        CreatedAtUtc = DateTimeOffset.UtcNow,
        IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
    };

    public bool IsReplied => RepliedAtUtc is not null;

    public void Reply(string replyText)
    {
        if (string.IsNullOrWhiteSpace(replyText))
            throw new DomainException("invalid_reply", "Yanıt metni boş olamaz.");

        ReplyText = replyText.Trim();
        RepliedAtUtc = DateTimeOffset.UtcNow;
    }
}

public interface ISiteMessageRepository
{
    Task<SiteMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<SiteMessage>> GetPageAsync(
        DateTimeOffset? cursorAtUtc, Guid? cursorId, int take, CancellationToken cancellationToken);

    void Add(SiteMessage message);
}
