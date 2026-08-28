using System.Security.Cryptography;
using System.Text;

namespace RowingClub.Identity.Infrastructure.Billing.Iyzico;

/// <summary>
/// iyzico'nun v2 API'leri (Abonelik dahil) için kullandığı IYZWSv2 istek imzalama şeması
/// (docs.iyzico.com/en/getting-started/preliminaries/authentication/hmacsha256-auth, 2026-08
/// itibarıyla doğrulandı). Adımlar:
/// 1. randomKey üretilir (burada: Guid, dokümandaki "timestamp+ardışık sayı" örneğiyle aynı amaçla - tekillik yeterli).
/// 2. imzalanacak metin = randomKey + uriPath (sorgu dizesi HARİÇ) + requestBody (tam olarak gönderilen JSON).
/// 3. HMAC-SHA256(secretKey, metin) hex string olarak hesaplanır.
/// 4. "apiKey:{apiKey}&amp;randomKey:{randomKey}&amp;signature:{hexSignature}" metni Base64'e çevrilir.
/// 5. Authorization header'ı: "IYZWSv2 {base64}"; ayrıca x-iyzi-rnd header'ı randomKey değerini taşır.
/// </summary>
public static class IyzicoRequestSigner
{
    public sealed record SignedHeaders(string Authorization, string RandomKey);

    public static SignedHeaders Sign(IyzicoOptions options, string uriPath, string requestBody)
    {
        var randomKey = Guid.NewGuid().ToString("N");
        var payload = randomKey + uriPath + requestBody;

        var hashBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.SecretKey), Encoding.UTF8.GetBytes(payload));
        var hexSignature = Convert.ToHexStringLower(hashBytes);

        var authorizationString = $"apiKey:{options.ApiKey}&randomKey:{randomKey}&signature:{hexSignature}";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorizationString));

        return new SignedHeaders($"IYZWSv2 {base64}", randomKey);
    }
}
