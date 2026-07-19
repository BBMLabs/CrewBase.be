using System.Security.Cryptography;
using System.Text;

namespace RowingClub.BuildingBlocks.Security.Tokens;

public sealed class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
