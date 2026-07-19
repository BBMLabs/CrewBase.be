namespace RowingClub.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Resolved from the validated access token by API middleware. Application handlers must treat
/// this as the only trusted source of "who is calling" - never re-read claims from HttpContext
/// directly (spec section 11 - "Kritik yetkilendirme sadece token içindeki role güvenmemelidir").
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid UserId { get; }

    string Email { get; }

    bool IsPlatformAdmin { get; }
}
