using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Billing;

public enum IyzicoUpgradePeriod
{
    Now,
    NextPeriod,
}

public sealed record IyzicoCheckoutFormResult(string CheckoutFormContent, string Token);

public sealed record IyzicoSubscriptionResult(
    bool Success,
    string? CustomerReferenceCode,
    string? SubscriptionReferenceCode,
    DateTimeOffset? CurrentPeriodEndUtc,
    decimal? ChargedAmount,
    string? PaymentReferenceCode,
    string? ErrorMessage);

/// <summary>
/// iyzico Abonelik (Subscription) API'sinin bu üründeki ihtiyacımız kadarını soyutlar. Kartın
/// kendisi hiçbir zaman bize gelmez/saklanmaz - iyzico tokenize eder, biz yalnızca referans
/// kodlarıyla çalışırız (PCI kapsamı bize geçmez).
/// </summary>
public interface IIyzicoSubscriptionClient
{
    bool IsConfigured { get; }

    /// <summary>Bu paket için iyzico Pricing Plan referans kodu yapılandırılmışsa true (Mico/Custom için her zaman false).</summary>
    bool SupportsPlan(CompanyPlan plan);

    /// <summary>İlk abonelik başlatma: checkout formu döner, müşteri kart bilgisini iyzico'nun formunda girer.</summary>
    Task<IyzicoCheckoutFormResult> InitializeCheckoutFormAsync(
        Guid companyId, string companyName, string contactEmail, CompanyPlan plan,
        string callbackUrl, CancellationToken cancellationToken);

    /// <summary>Checkout formundan dönülen token ile sonucu doğrular ve abonelik/ilk tahsilat detaylarını getirir.</summary>
    Task<IyzicoSubscriptionResult> RetrieveCheckoutFormResultAsync(string token, CancellationToken cancellationToken);

    /// <summary>
    /// Hem yükseltme hem düşürme aynı uçtan yapılır; bu üründe düşürme her zaman kendi
    /// zamanlayıcımızla dönem sonunda çağrılır, o yüzden burada her zaman <see cref="IyzicoUpgradePeriod.Now"/> kullanılır.
    /// </summary>
    Task<IyzicoSubscriptionResult> UpgradeSubscriptionAsync(
        string subscriptionReferenceCode, CompanyPlan newPlan,
        IyzicoUpgradePeriod period, CancellationToken cancellationToken);

    Task<bool> CancelSubscriptionAsync(string subscriptionReferenceCode, CancellationToken cancellationToken);

    /// <summary>Bir yenileme webhook'unun tahsilat tutarını/sonucunu almak için kullanılır (webhook payload'ı tutar taşımıyor).</summary>
    Task<IyzicoSubscriptionResult> RetrieveSubscriptionAsync(string subscriptionReferenceCode, CancellationToken cancellationToken);
}
