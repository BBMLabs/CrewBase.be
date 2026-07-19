namespace RowingClub.BuildingBlocks.Security.Tokens;

/// <summary>
/// Refresh tokens are already high-entropy random values, so a fast cryptographic hash (not a
/// slow password hash) is sufficient and keeps lookups by hash cheap (spec section 11 -
/// "Refresh token hashlenmiş olarak saklanmalıdır").
/// </summary>
public interface IRefreshTokenHasher
{
    string Hash(string rawToken);
}
