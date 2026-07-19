using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace RowingClub.BuildingBlocks.Security.Jwt;

public sealed class RsaJwtTokenService : IJwtTokenService, IDisposable
{
    private readonly JwtOptions _options;
    private readonly RSA _privateKey;
    private readonly JwtSecurityTokenHandler _handler = new();

    public RsaJwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _privateKey = RSA.Create();
        _privateKey.ImportFromPem(_options.SigningPrivateKeyPem);
    }

    public IssuedAccessToken IssueAccessToken(
        Guid userId,
        string email,
        IReadOnlyCollection<Claim>? extraClaims = null)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(_options.AccessTokenLifetime);
        var jwtId = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        if (extraClaims is not null)
        {
            claims.AddRange(extraClaims);
        }

        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(_privateKey) { KeyId = _options.KeyId },
            SecurityAlgorithms.RsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false },
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);

        return new IssuedAccessToken(_handler.WriteToken(token), expiresAt, jwtId);
    }

    public void Dispose() => _privateKey.Dispose();
}
