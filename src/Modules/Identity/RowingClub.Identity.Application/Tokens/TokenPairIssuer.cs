using System.Security.Claims;
using Microsoft.Extensions.Options;
using RowingClub.BuildingBlocks.Security.Jwt;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.Identity.Application.Tokens;

public sealed record TokenPair(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken);

public sealed class TokenPairIssuer(
    IJwtTokenService jwtTokenService,
    IOpaqueTokenGenerator opaqueTokenGenerator,
    IRefreshTokenHasher refreshTokenHasher,
    IRefreshTokenRepository refreshTokenRepository,
    IOptions<IdentityOptions> identityOptions)
{
    private readonly IdentityOptions _options = identityOptions.Value;

    public (TokenPair Pair, RefreshToken IssuedRefreshToken) IssueNewFamily(
        Guid userId, string email, string role, Guid? companyId = null)
    {
        var extraClaims = BuildClaims(role, companyId);
        var accessToken = jwtTokenService.IssueAccessToken(userId, email, extraClaims);
        var rawRefreshToken = opaqueTokenGenerator.Generate();
        var refreshToken = RefreshToken.IssueNewFamily(
            userId, refreshTokenHasher.Hash(rawRefreshToken), _options.RefreshTokenLifetime);

        refreshTokenRepository.Add(refreshToken);

        return (new TokenPair(accessToken.Token, accessToken.ExpiresAtUtc, rawRefreshToken), refreshToken);
    }

    public (TokenPair Pair, RefreshToken IssuedRefreshToken) RotateWithinFamily(
        Guid userId, string email, Guid familyId, string role, Guid? companyId = null)
    {
        var extraClaims = BuildClaims(role, companyId);
        var accessToken = jwtTokenService.IssueAccessToken(userId, email, extraClaims);
        var rawRefreshToken = opaqueTokenGenerator.Generate();
        var refreshToken = RefreshToken.IssueInFamily(
            userId, familyId, refreshTokenHasher.Hash(rawRefreshToken), _options.RefreshTokenLifetime);

        refreshTokenRepository.Add(refreshToken);

        return (new TokenPair(accessToken.Token, accessToken.ExpiresAtUtc, rawRefreshToken), refreshToken);
    }

    private static IReadOnlyCollection<Claim> BuildClaims(string role, Guid? companyId)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };

        if (companyId.HasValue)
        {
            claims.Add(new Claim("company_id", companyId.Value.ToString()));
        }

        return claims;
    }
}
