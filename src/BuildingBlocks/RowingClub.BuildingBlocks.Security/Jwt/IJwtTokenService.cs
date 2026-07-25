using System.Security.Claims;

namespace RowingClub.BuildingBlocks.Security.Jwt;

public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAtUtc, string JwtId);

public interface IJwtTokenService
{
    IssuedAccessToken IssueAccessToken(Guid userId, string email, IReadOnlyCollection<Claim>? extraClaims = null);
}
