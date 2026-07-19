using System.ComponentModel.DataAnnotations;

namespace RowingClub.BuildingBlocks.Infrastructure.Idempotency;

/// <summary>Bound from REDIS_HOST/REDIS_PORT/REDIS_USERNAME/REDIS_PASSWORD (spec section 10). The
/// backend builds the actual endpoint from these pieces rather than taking a pre-assembled
/// connection string. Username/password are optional - a local dev Redis typically runs without
/// auth.</summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    [Required] public required string Host { get; init; }

    [Range(1, 65535)] public required int Port { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }
}
