namespace RowingClub.BuildingBlocks.Security.Encryption;

/// <summary>
/// Deterministic HMAC over a normalized value, stored alongside the AES-GCM ciphertext so an
/// encrypted field can still be looked up by exact match without ever keeping it as plaintext
/// (spec section 9 - "Gerekiyorsa normalize edilmiş değer üzerinden ayrı bir HMAC tabanlı blind
/// index üret").
/// </summary>
public interface IBlindIndexer
{
    string ComputeBlindIndex(string normalizedValue);
}
