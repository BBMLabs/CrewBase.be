namespace RowingClub.Identity.Domain.Users;

public sealed class UserRecoveryCode
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string CodeHash { get; private set; } = null!;

    public bool IsUsed { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    private UserRecoveryCode()
    {
    }

    private UserRecoveryCode(Guid userId, string codeHash)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CodeHash = codeHash;
        IsUsed = false;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static UserRecoveryCode Create(Guid userId, string codeHash) => new(userId, codeHash);

    public void MarkUsed()
    {
        if (IsUsed) return;
        IsUsed = true;
        UsedAtUtc = DateTimeOffset.UtcNow;
    }
}
