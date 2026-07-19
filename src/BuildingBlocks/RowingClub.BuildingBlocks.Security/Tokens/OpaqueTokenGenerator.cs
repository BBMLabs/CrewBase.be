using System.Security.Cryptography;

namespace RowingClub.BuildingBlocks.Security.Tokens;

public sealed class OpaqueTokenGenerator : IOpaqueTokenGenerator
{
    private const int TokenSizeBytes = 32; // 256-bit

    public string Generate() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenSizeBytes))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
