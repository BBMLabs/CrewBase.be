using System.Security.Cryptography;

namespace RowingClub.Scheduling.Domain.Branches;

/// <summary>
/// 6 rakam + 2 büyük harf biçiminde şube kodu üretir (ör. "482913QK") - şubenin kendi genel
/// sitesinin adresinde kullanılır. Karışan harfler (I/O) alfabede yok. Çakışma olasılığı çok
/// düşüktür; çağıran veritabanı benzersizliğine karşı yeniden üretim döngüsüyle destekler.
/// </summary>
public static class BranchCodeGenerator
{
    private const string LetterAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ";

    public static string Generate()
    {
        Span<char> chars = stackalloc char[8];
        for (var i = 0; i < 6; i++)
            chars[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        for (var i = 6; i < 8; i++)
            chars[i] = LetterAlphabet[RandomNumberGenerator.GetInt32(LetterAlphabet.Length)];

        return new string(chars);
    }
}
