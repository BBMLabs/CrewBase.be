using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace RowingClub.BuildingBlocks.Security.Encryption;

public sealed class HmacBlindIndexer : IBlindIndexer
{
    private readonly byte[] _key;

    public HmacBlindIndexer(IOptions<EncryptionOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.BlindIndexKey);
    }

    public string ComputeBlindIndex(string normalizedValue) =>
        Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(normalizedValue)));
}
