using System.Security.Cryptography;
using System.Text;

namespace RowingClub.Identity.Infrastructure.Billing.Iyzico;

/// <summary>
/// iyzico Abonelik webhook'unun X-IYZ-SIGNATURE-V3 imzasını doğrular
/// (docs.iyzico.com/en/advanced/webhook, 2026-08 itibarıyla doğrulandı - X-Iyz-Signature ve
/// V2 artık desteklenmiyor, yalnızca V3 kullanılmalı). Fail-closed: WebhookSecret ya da
/// MerchantId boşsa (yapılandırılmamış) HER ZAMAN false döner - asla sessizce "geçti" saymaz.
/// </summary>
public static class IyzicoWebhookSignatureVerifier
{
    public static bool Verify(
        IyzicoOptions options,
        string eventType,
        string subscriptionReferenceCode,
        string orderReferenceCode,
        string customerReferenceCode,
        string receivedSignatureHex)
    {
        if (string.IsNullOrWhiteSpace(options.WebhookSecret) || string.IsNullOrWhiteSpace(options.MerchantId))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(receivedSignatureHex))
        {
            return false;
        }

        var payload = options.MerchantId + options.WebhookSecret + eventType
            + subscriptionReferenceCode + orderReferenceCode + customerReferenceCode;

        var expectedBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.WebhookSecret), Encoding.UTF8.GetBytes(payload));

        if (!TryDecodeHex(receivedSignatureHex, out var receivedBytes))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, receivedBytes);
    }

    private static bool TryDecodeHex(string hex, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromHexString(hex);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}
