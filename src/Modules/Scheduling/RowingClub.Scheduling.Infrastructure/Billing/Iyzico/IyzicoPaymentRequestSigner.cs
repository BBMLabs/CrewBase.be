using System.Security.Cryptography;
using System.Text;

namespace RowingClub.Scheduling.Infrastructure.Billing.Iyzico;

/// <summary>
/// IYZWSv2 istek imzalama şeması (docs.iyzico.com/en/getting-started/preliminaries/authentication/hmacsha256-auth) -
/// Identity.Infrastructure.Billing.Iyzico.IyzicoRequestSigner ile BİREBİR AYNI algoritma, kasıtlı
/// olarak kopyalanmıştır (bkz. plan mimari karar #3 - iki modülü paylaşımlı bir BuildingBlocks
/// bağımlılığına bağlamak bu oturumun kapsamı dışında bırakıldı).
/// </summary>
public static class IyzicoPaymentRequestSigner
{
    public sealed record SignedHeaders(string Authorization, string RandomKey);

    public static SignedHeaders Sign(PackagePaymentOptions options, string uriPath, string requestBody)
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
