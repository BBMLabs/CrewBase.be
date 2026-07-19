using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace RowingClub.BuildingBlocks.Security.Encryption;

public sealed class AesGcmFieldEncryptor : IFieldEncryptor
{
    public const int NonceSizeBytes = 12;
    public const int TagSizeBytes = 16;

    private readonly EncryptionOptions _options;
    private readonly Dictionary<int, byte[]> _keysByVersion;

    public AesGcmFieldEncryptor(IOptions<EncryptionOptions> options)
    {
        _options = options.Value;
        _keysByVersion = _options.Keys.ToDictionary(
            kvp => kvp.Key,
            kvp => Convert.FromBase64String(kvp.Value));
    }

    public EncryptedValue Encrypt(string plaintext)
    {
        var key = _keysByVersion[_options.CurrentKeyVersion];
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherText = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Encrypt(nonce, plaintextBytes, cipherText, tag);

        return new EncryptedValue(cipherText, nonce, tag, _options.CurrentKeyVersion);
    }

    public string Decrypt(EncryptedValue value)
    {
        if (!_keysByVersion.TryGetValue(value.KeyVersion, out var key))
        {
            throw new InvalidOperationException(
                $"Şifreleme anahtarı bulunamadı: sürüm {value.KeyVersion}. Anahtar rotasyonu sırasında " +
                "eski anahtar erken kaldırılmış olabilir.");
        }

        var plaintextBytes = new byte[value.CipherText.Length];

        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Decrypt(value.Nonce, value.CipherText, value.Tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
