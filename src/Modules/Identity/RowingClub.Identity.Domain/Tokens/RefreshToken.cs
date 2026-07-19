using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Events;

namespace RowingClub.Identity.Domain.Tokens;

/// <summary>
/// One link in a rotation chain. All tokens issued from the same original login share
/// <see cref="FamilyId"/>; rotating replaces the current token and revokes it, keeping
/// <see cref="ReplacedByTokenId"/> as a pointer. If a REVOKED token is ever presented again, that
/// is theft/reuse - the caller (RefreshTokenCommandHandler) must revoke the whole family
/// (spec section 11 - "Token reuse tespit edilirse token ailesi iptal edilmelidir").
/// </summary>
public sealed class RefreshToken : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    /// <summary>SHA-256 hash of the opaque token value - the raw value is never persisted.</summary>
    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, Guid userId, Guid familyId, string tokenHash, DateTimeOffset expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static RefreshToken IssueNewFamily(Guid userId, string tokenHash, TimeSpan lifetime) =>
        new(Guid.NewGuid(), userId, Guid.NewGuid(), tokenHash, DateTimeOffset.UtcNow.Add(lifetime));

    public static RefreshToken IssueInFamily(Guid userId, Guid familyId, string tokenHash, TimeSpan lifetime) =>
        new(Guid.NewGuid(), userId, familyId, tokenHash, DateTimeOffset.UtcNow.Add(lifetime));

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow;

    public void Revoke() => RevokedAtUtc ??= DateTimeOffset.UtcNow;

    /// <summary>
    /// Called on the already-revoked token that was just presented again - a strong signal it was
    /// stolen. Raises the event that drives revoking the rest of the family (spec section 11).
    /// </summary>
    public void FlagReuse()
    {
        Revoke();
        RaiseDomainEvent(new RefreshTokenReuseDetectedDomainEvent(UserId, FamilyId));
    }

    /// <summary>Rotation: this token is consumed and points at its successor.</summary>
    public void MarkReplacedBy(Guid newTokenId)
    {
        Revoke();
        ReplacedByTokenId = newTokenId;
    }
}
