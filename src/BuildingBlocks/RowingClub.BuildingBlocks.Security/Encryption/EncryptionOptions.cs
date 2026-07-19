using System.ComponentModel.DataAnnotations;

namespace RowingClub.BuildingBlocks.Security.Encryption;

/// <summary>
/// Bound from FIELD_ENCRYPTION_KEY_* env vars (spec section 10). <see cref="Keys"/> must contain
/// at least the current version; the previous version is kept only so already-encrypted values
/// remain decryptable until they are re-encrypted under the new key (spec section 9 - key rotation).
/// </summary>
public sealed class EncryptionOptions
{
    public const string SectionName = "FieldEncryption";

    public required int CurrentKeyVersion { get; init; }

    /// <summary>Key version -> base64-encoded 256-bit AES key.</summary>
    [Required] public required IReadOnlyDictionary<int, string> Keys { get; init; }

    /// <summary>Base64-encoded HMAC key used for blind-index hashing of searchable encrypted fields.</summary>
    [Required] public required string BlindIndexKey { get; init; }
}
