using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Identity.Domain.Tokens;

public sealed class EmailVerificationToken : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    private EmailVerificationToken()
    {
    }

    private EmailVerificationToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAtUtc) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public static EmailVerificationToken Issue(Guid userId, string tokenHash, TimeSpan lifetime) =>
        new(Guid.NewGuid(), userId, tokenHash, DateTimeOffset.UtcNow.Add(lifetime));

    public bool IsValid => UsedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow;

    public void MarkUsed()
    {
        if (!IsValid)
        {
            throw new DomainException(
                "email_verification_token_invalid",
                "Doğrulama bağlantısı geçersiz veya süresi dolmuş.");
        }

        UsedAtUtc = DateTimeOffset.UtcNow;
    }
}
