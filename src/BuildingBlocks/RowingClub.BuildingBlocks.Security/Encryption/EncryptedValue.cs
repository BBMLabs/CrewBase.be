namespace RowingClub.BuildingBlocks.Security.Encryption;

/// <summary>
/// Everything needed to decrypt a single AES-256-GCM encrypted field, per spec section 9: cipher
/// text, nonce, authentication tag and the key version it was encrypted under. Persisted as one
/// opaque column via <see cref="Serialize"/> / <see cref="Deserialize"/>.
/// </summary>
public sealed record EncryptedValue(byte[] CipherText, byte[] Nonce, byte[] Tag, int KeyVersion)
{
    public string Serialize()
    {
        var keyVersionBytes = BitConverter.GetBytes(KeyVersion);
        var combined = new byte[4 + Nonce.Length + Tag.Length + CipherText.Length];

        Buffer.BlockCopy(keyVersionBytes, 0, combined, 0, 4);
        Buffer.BlockCopy(Nonce, 0, combined, 4, Nonce.Length);
        Buffer.BlockCopy(Tag, 0, combined, 4 + Nonce.Length, Tag.Length);
        Buffer.BlockCopy(CipherText, 0, combined, 4 + Nonce.Length + Tag.Length, CipherText.Length);

        return Convert.ToBase64String(combined);
    }

    public static EncryptedValue Deserialize(string serialized)
    {
        var combined = Convert.FromBase64String(serialized);

        var keyVersion = BitConverter.ToInt32(combined, 0);
        var nonce = combined[4..(4 + AesGcmFieldEncryptor.NonceSizeBytes)];
        var tagStart = 4 + AesGcmFieldEncryptor.NonceSizeBytes;
        var tag = combined[tagStart..(tagStart + AesGcmFieldEncryptor.TagSizeBytes)];
        var cipherStart = tagStart + AesGcmFieldEncryptor.TagSizeBytes;
        var cipherText = combined[cipherStart..];

        return new EncryptedValue(cipherText, nonce, tag, keyVersion);
    }
}
