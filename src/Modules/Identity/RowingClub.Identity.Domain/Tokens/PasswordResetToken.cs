using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Identity.Domain.Tokens;

public sealed class PasswordResetToken : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    private PasswordResetToken()
    {
    }

    private PasswordResetToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAtUtc) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public static PasswordResetToken Issue(Guid userId, string tokenHash, TimeSpan lifetime) =>
        new(Guid.NewGuid(), userId, tokenHash, DateTimeOffset.UtcNow.Add(lifetime));

    public bool IsValid => UsedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow;

    public void MarkUsed()
    {
        if (!IsValid)
        {
            throw new DomainException(
                "password_reset_token_invalid",
                "Parola sıfırlama bağlantısı geçersiz veya süresi dolmuş.");
        }

        UsedAtUtc = DateTimeOffset.UtcNow;
    }
}
