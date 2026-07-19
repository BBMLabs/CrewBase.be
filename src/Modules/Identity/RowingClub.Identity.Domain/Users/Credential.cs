using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Events;

namespace RowingClub.Identity.Domain.Users;

/// <summary>
/// The user's password credential. Kept as its own aggregate (not a child of <see cref="User"/>)
/// because password changes/resets are their own consistency boundary and don't need the rest of
/// the user loaded. Only ever holds an already-hashed value - hashing itself is an application/
/// infrastructure concern (spec section 9 - "Parolalar şifrelenerek değil hashlenerek saklanmalıdır").
/// </summary>
public sealed class Credential : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    public string PasswordHash { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ChangedAtUtc { get; private set; }

    private Credential()
    {
    }

    private Credential(Guid id, Guid userId, string passwordHash) : base(id)
    {
        UserId = userId;
        PasswordHash = passwordHash;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Credential Create(Guid userId, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("password_hash_required", "Parola hash değeri boş olamaz.");
        }

        return new Credential(Guid.NewGuid(), userId, passwordHash);
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new DomainException("password_hash_required", "Parola hash değeri boş olamaz.");
        }

        PasswordHash = newPasswordHash;
        ChangedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new CredentialPasswordChangedDomainEvent(UserId));
    }
}
