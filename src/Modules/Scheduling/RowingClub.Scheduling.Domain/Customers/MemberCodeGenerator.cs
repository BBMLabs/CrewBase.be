using System.Security.Cryptography;

namespace RowingClub.Scheduling.Domain.Customers;

/// <summary>
/// 2 harf + 6 rakamdan oluşan üye kodu üretir (ör. "AB123456"). Karışan karakterler (I/O/L)
/// harf alfabesinde yok; kriptografik rastgelelik kullanılır. Çağıran, veritabanı
/// benzersizliğine karşı yeniden üretim döngüsüyle destekler (bkz. MemberCodeAssigner) -
/// böylece iki üye asla aynı kodu paylaşamaz.
/// </summary>
public static class MemberCodeGenerator
{
    private const string LetterAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ";

    public static string Generate()
    {
        Span<char> chars = stackalloc char[8];
        for (var i = 0; i < 2; i++)
            chars[i] = LetterAlphabet[RandomNumberGenerator.GetInt32(LetterAlphabet.Length)];
        for (var i = 2; i < 8; i++)
            chars[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));

        return new string(chars);
    }

    public static string Normalize(string raw) => raw.Trim().ToUpperInvariant();
}
