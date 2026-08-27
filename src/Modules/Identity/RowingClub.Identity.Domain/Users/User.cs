using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Events;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Domain.Users;

public enum UserStatus
{
    Active = 0,
    Deactivated = 1,
}

public enum UserRole
{
    PlatformAdmin = 0,
    CompanyAdmin = 1,
    Employee = 2,
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

    public UserRole Role { get; private set; }

    public Guid? CompanyId { get; private set; }

    public UserStatus Status { get; private set; }

    public bool TwoFactorEnabled { get; private set; }

    public string TwoFactorMethod { get; private set; } = "None";

    public string? TwoFactorSecret { get; private set; }

    public int FailedLoginAttemptCount { get; private set; }

    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    private User()
    {
    }

    private User(Guid id, EmailAddress email, UserRole role, Guid? companyId) : base(id)
    {
        Email = email;
        Role = role;
        CompanyId = companyId;
        Status = UserStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static User Register(EmailAddress email)
    {
        var user = new User(Guid.NewGuid(), email, UserRole.Employee, null);
        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, email.Value));
        return user;
    }

    public static User RegisterCompanyAdmin(EmailAddress email, Guid companyId)
    {
        var user = new User(Guid.NewGuid(), email, UserRole.CompanyAdmin, companyId);
        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, email.Value));
        return user;
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
    }

    public void Block() => Status = UserStatus.Deactivated;

    public void Unblock() => Status = UserStatus.Active;

    public void EnableTwoFactor(string method, string? secret = null)
    {
        TwoFactorEnabled = true;
        TwoFactorMethod = method;
        TwoFactorSecret = secret;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorMethod = "None";
        TwoFactorSecret = null;
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
                "Hesabınız çok fazla başarısız giriş denemesi nedeniyle geçici olarak kilitlenmiştir. Lütfen daha sonra tekrar deneyiniz.");
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
