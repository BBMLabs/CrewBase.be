using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Identity.Domain.Tokens;

/// <summary>
/// One logged-in device/client, tracked by which refresh token family it is using so "logout this
/// device" (revoke one family) and "logout everywhere" (revoke every family for the user) are both
/// simple lookups (spec section 6.1 - "Cihaz ve oturum yönetimi").
/// </summary>
public sealed class UserSession : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    public Guid RefreshTokenFamilyId { get; private set; }

    public string? DeviceInfo { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset LastSeenAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    private UserSession()
    {
    }

    private UserSession(Guid id, Guid userId, Guid refreshTokenFamilyId, string? deviceInfo) : base(id)
    {
        UserId = userId;
        RefreshTokenFamilyId = refreshTokenFamilyId;
        DeviceInfo = deviceInfo;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        LastSeenAtUtc = CreatedAtUtc;
    }

    public static UserSession Start(Guid userId, Guid refreshTokenFamilyId, string? deviceInfo) =>
        new(Guid.NewGuid(), userId, refreshTokenFamilyId, deviceInfo);

    public bool IsActive => RevokedAtUtc is null;

    public void Touch() => LastSeenAtUtc = DateTimeOffset.UtcNow;

    public void Revoke() => RevokedAtUtc ??= DateTimeOffset.UtcNow;
}
