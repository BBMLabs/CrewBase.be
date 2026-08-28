using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Infrastructure.Billing.Iyzico;

/// <summary>
/// iyzico Abonelik (Subscription) API yapılandırması. Bir mağaza hesabı ve Tayfa/Kaptan/Amiral
/// için 3 ayrı Pricing Plan oluşturmak kullanıcının kendisinin yapması gereken bir şey (bkz.
/// docs.iyzico.com panel/API) - bu değerler boşken abonelik özellikleri "faturalama
/// yapılandırılmamış" hatasıyla düzgünce devre dışı kalır, aynı RECAPTCHA_SECRET_KEY deseninde.
/// </summary>
public sealed class IyzicoOptions
{
    public const string SectionName = "Iyzico";

    public string ApiKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>iyzico panelinden alınan mağaza kimliği - webhook imza doğrulamasında kullanılır.</summary>
    public string MerchantId { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "https://sandbox-api.iyzipay.com";

    /// <summary>Webhook imzasını (X-IYZ-SIGNATURE-V3) doğrulamak için kullanılan paylaşılan sır.</summary>
    public string WebhookSecret { get; init; } = string.Empty;

    public string PlanReferenceCodeTayfa { get; init; } = string.Empty;
    public string PlanReferenceCodeKaptan { get; init; } = string.Empty;
    public string PlanReferenceCodeAmiral { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(SecretKey);

    /// <summary>Mico/Custom için null döner (iyzico'da karşılığı yok - Mico ücretsiz, Custom self-servis değil).</summary>
    public string? PlanReferenceCodeFor(CompanyPlan plan) => plan switch
    {
        CompanyPlan.Tayfa => NullIfEmpty(PlanReferenceCodeTayfa),
        CompanyPlan.Kaptan => NullIfEmpty(PlanReferenceCodeKaptan),
        CompanyPlan.Amiral => NullIfEmpty(PlanReferenceCodeAmiral),
        _ => null,
    };

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
