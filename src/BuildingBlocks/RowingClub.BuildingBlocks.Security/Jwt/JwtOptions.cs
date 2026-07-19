using System.ComponentModel.DataAnnotations;

namespace RowingClub.BuildingBlocks.Security.Jwt;

/// <summary>Bound from JWT_* env vars (spec section 10) - never hardcode these. Validated on
/// startup so a missing secret fails fast instead of surfacing as a confusing 500 on first login
/// (spec section 10 - "Uygulama eksik zorunlu secret durumunda fail-fast olmalıdır").</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public required string Issuer { get; init; }

    [Required] public required string Audience { get; init; }

    /// <summary>PEM-encoded RSA private key, used to sign access tokens (RS256).</summary>
    [Required] public required string SigningPrivateKeyPem { get; init; }

    /// <summary>PEM-encoded RSA public key, used to validate access tokens (RS256).</summary>
    [Required] public required string SigningPublicKeyPem { get; init; }

    /// <summary>Key identifier carried in the token header so a future key rotation can be
    /// recognised by validators without breaking already-issued tokens (spec section 10).</summary>
    [Required] public required string KeyId { get; init; }

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);
}
