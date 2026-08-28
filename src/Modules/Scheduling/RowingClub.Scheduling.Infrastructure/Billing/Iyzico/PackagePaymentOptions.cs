namespace RowingClub.Scheduling.Infrastructure.Billing.Iyzico;

/// <summary>
/// Üye ders paketi satın alma için iyzico klasik Payment/CheckoutForm API yapılandırması. Firma
/// aboneliği (Identity modülü, Abonelik API'si) ile AYNI iyzico mağaza hesabı/env değişkenleri
/// paylaşılır (IYZICO_API_KEY/IYZICO_SECRET_KEY/IYZICO_BASE_URL) - bkz. plan mimari karar #3.
/// Identity.Infrastructure.Billing.Iyzico.IyzicoOptions'tan kasıtlı olarak AYRI bir sınıf: iki
/// modülü ortak bir bağımlılığa bağlamamak için (bkz. IyzicoPaymentRequestSigner'daki aynı gerekçe).
/// </summary>
public sealed class PackagePaymentOptions
{
    public const string SectionName = "PackagePayment";

    public string ApiKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://sandbox-api.iyzipay.com";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(SecretKey);
}
