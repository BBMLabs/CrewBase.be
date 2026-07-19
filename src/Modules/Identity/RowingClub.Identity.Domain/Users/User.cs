using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Events;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Domain.Users;

public enum UserStatus
{
    Active = 0,
    Deactivated = 1,
}

/// <summary>
/// Platform-level account identity. Deliberately has no ClubId - a user can belong to many clubs
/// with different roles in each (spec section 2, kural 1-2); club membership is a separate
/// bounded context (Memberships).
/// </summary>
public sealed class User : AggregateRoot<Guid>
{
    public EmailAddress Email { get; private set; } = null!;

    public bool EmailVerified { get; private set; }

    public UserStatus Status { get; private set; }

    public int FailedLoginAttemptCount { get; private set; }

    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    private User()
    {
    }

    private User(Guid id, EmailAddress email) : base(id)
    {
        Email = email;
        Status = UserStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static User Register(EmailAddress email)
    {
        var user = new User(Guid.NewGuid(), email);
        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, email.Value));
        return user;
    }

    public void VerifyEmail()
    {
        if (EmailVerified)
        {
            return;
        }

        EmailVerified = true;
        RaiseDomainEvent(new UserEmailVerifiedDomainEvent(Id));
    }

    public bool IsLockedOut() => LockedUntilUtc is not null && LockedUntilUtc.Value > DateTimeOffset.UtcNow;

    public void EnsureCanAuthenticate()
    {
        if (Status == UserStatus.Deactivated)
        {
            throw new DomainException("account_deactivated", "Hesap devre dışı bırakılmış.");
        }

        if (IsLockedOut())
        {
            throw new DomainException(
                "account_locked",
                $"Hesap {LockedUntilUtc:O} tarihine kadar kilitli. Çok fazla başarısız giriş denemesi.");
        }
    }

    public void RegisterFailedLogin(int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttemptCount++;

        if (FailedLoginAttemptCount >= maxAttempts)
        {
            LockedUntilUtc = DateTimeOffset.UtcNow.Add(lockoutDuration);
            RaiseDomainEvent(new UserLockedOutDomainEvent(Id, LockedUntilUtc.Value));
        }
    }

    public void RegisterSuccessfulLogin()
    {
        FailedLoginAttemptCount = 0;
        LockedUntilUtc = null;
        LastLoginAtUtc = DateTimeOffset.UtcNow;
    }
}
