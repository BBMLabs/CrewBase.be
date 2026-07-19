using Microsoft.Extensions.Options;
using RowingClub.BuildingBlocks.Security.Jwt;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.Identity.Application.Tokens;

public sealed record TokenPair(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken);

/// <summary>
/// Shared by Login (starts a new refresh token family) and Refresh (rotates within an existing
/// family) so the access+refresh issuing logic exists in exactly one place.
/// </summary>
public sealed class TokenPairIssuer(
    IJwtTokenService jwtTokenService,
    IOpaqueTokenGenerator opaqueTokenGenerator,
    IRefreshTokenHasher refreshTokenHasher,
    IRefreshTokenRepository refreshTokenRepository,
    IOptions<IdentityOptions> identityOptions)
{
    private readonly IdentityOptions _options = identityOptions.Value;

    public (TokenPair Pair, RefreshToken IssuedRefreshToken) IssueNewFamily(Guid userId, string email)
    {
        var accessToken = jwtTokenService.IssueAccessToken(userId, email);
        var rawRefreshToken = opaqueTokenGenerator.Generate();
        var refreshToken = RefreshToken.IssueNewFamily(
            userId, refreshTokenHasher.Hash(rawRefreshToken), _options.RefreshTokenLifetime);

        refreshTokenRepository.Add(refreshToken);

        return (new TokenPair(accessToken.Token, accessToken.ExpiresAtUtc, rawRefreshToken), refreshToken);
    }

    public (TokenPair Pair, RefreshToken IssuedRefreshToken) RotateWithinFamily(
        Guid userId, string email, Guid familyId)
    {
        var accessToken = jwtTokenService.IssueAccessToken(userId, email);
        var rawRefreshToken = opaqueTokenGenerator.Generate();
        var refreshToken = RefreshToken.IssueInFamily(
            userId, familyId, refreshTokenHasher.Hash(rawRefreshToken), _options.RefreshTokenLifetime);

        refreshTokenRepository.Add(refreshToken);

        return (new TokenPair(accessToken.Token, accessToken.ExpiresAtUtc, rawRefreshToken), refreshToken);
    }
}
