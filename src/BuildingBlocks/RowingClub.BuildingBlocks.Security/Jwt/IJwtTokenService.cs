using System.Security.Claims;

namespace RowingClub.BuildingBlocks.Security.Jwt;

public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAtUtc, string JwtId);

/// <summary>
/// Issues short-lived RS256 access tokens carrying only <c>sub</c>, <c>jti</c>, <c>iss</c>,
/// <c>aud</c>, <c>exp</c>, <c>iat</c> and the caller-supplied claims (spec section 11). Role and
/// tenant membership are intentionally NOT baked into the token as trusted facts - every request
/// re-validates them server-side via <c>ICurrentTenant</c> resolution.
/// </summary>
public interface IJwtTokenService
{
    IssuedAccessToken IssueAccessToken(Guid userId, string email, IReadOnlyCollection<Claim>? extraClaims = null);
}
