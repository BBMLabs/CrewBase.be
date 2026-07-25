namespace RowingClub.Identity.Domain.Tokens;

public sealed class PendingTwoFactorToken
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public string DeviceInfo { get; private set; } = null!;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public bool IsUsed { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private PendingTwoFactorToken()
    {
    }

    private PendingTwoFactorToken(Guid userId, string tokenHash, string deviceInfo, TimeSpan lifetime)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        DeviceInfo = deviceInfo;
        ExpiresAtUtc = DateTimeOffset.UtcNow.Add(lifetime);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static PendingTwoFactorToken Create(Guid userId, string tokenHash, string deviceInfo, TimeSpan lifetime)
        => new(userId, tokenHash, deviceInfo, lifetime);

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAtUtc;

    public bool IsValid => !IsUsed && !IsExpired;

    public void MarkUsed()
    {
        IsUsed = true;
    }
}
