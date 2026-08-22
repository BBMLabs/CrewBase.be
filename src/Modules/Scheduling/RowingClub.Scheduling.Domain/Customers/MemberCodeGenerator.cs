using System.Security.Cryptography;

namespace RowingClub.Scheduling.Domain.Customers;

/// <summary>
/// KRK-XXXXXX biçiminde üye kodu üretir. Karışan karakterler (0/O, 1/I/L) alfabede yok;
/// kriptografik rastgelelik kullanılır. Çakışma olasılığı 30^6'da 1'in altındadır ve çağıran,
/// veritabanı benzersizliğine karşı yeniden üretim döngüsüyle destekler.
/// </summary>
public static class MemberCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string Generate()
    {
        Span<char> chars = stackalloc char[6];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return "KRK-" + new string(chars);
    }

    public static string Normalize(string raw) => raw.Trim().ToUpperInvariant();
}
