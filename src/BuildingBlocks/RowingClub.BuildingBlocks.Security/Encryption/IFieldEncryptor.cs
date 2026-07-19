namespace RowingClub.BuildingBlocks.Security.Encryption;

/// <summary>
/// Application-level encryption for sensitive fields (spec section 9): phone, address, emergency
/// contact, health declarations, ID/document numbers, private member/lesson notes. Never store
/// these fields as plaintext.
/// </summary>
public interface IFieldEncryptor
{
    EncryptedValue Encrypt(string plaintext);

    string Decrypt(EncryptedValue value);
}
