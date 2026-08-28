using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RowingClub.Identity.Application.Billing;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Infrastructure.Billing.Iyzico;

/// <summary>
/// iyzico Abonelik (Subscription) v2 API istemcisi. Yükseltme/İptal/Retry uçları
/// docs.iyzico.com/urunler/abonelik/abonelik-entegrasyonu/abonelik-islemleri'ne göre birebir
/// doğrulanmıştır (2026-08). Checkout-form başlatma/sonuç alma ve abonelik detayı getirme uçları
/// ise iyzico'nun v1 checkout-form API'sindeki tutarlı isimlendirme deseninden türetilmiştir -
/// canlıya almadan önce docs.iyzico.com/en/products/subscription üzerinden path'lerin birebir
/// teyit edilmesi gerekir (aşağıda ilgili metotlarda ayrıca işaretlendi).
/// </summary>
public sealed class IyzicoSubscriptionClient(
    HttpClient httpClient, IOptions<IyzicoOptions> options, ILogger<IyzicoSubscriptionClient> logger)
    : IIyzicoSubscriptionClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IyzicoOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public bool SupportsPlan(CompanyPlan plan) => IsConfigured && _options.PlanReferenceCodeFor(plan) is not null;

    public async Task<IyzicoCheckoutFormResult> InitializeCheckoutFormAsync(
        Guid companyId, string companyName, string contactEmail, CompanyPlan plan,
        string callbackUrl, CancellationToken cancellationToken)
    {
        var planReferenceCode = EnsurePlanConfigured(plan);

        // DOĞRULA: iyzico'nun resmi abonelik dokümanına göre kesin path/alan adları teyit edilmeli.
        const string path = "/v2/subscription/checkoutform/initialize";
        var body = new
        {
            locale = "tr",
            conversationId = companyId.ToString(),
            callbackUrl,
            pricingPlanReferenceCode = planReferenceCode,
            customer = new
            {
                name = companyName,
                surname = "-",
                email = contactEmail,
                identityNumber = "11111111111",
                shippingAddress = new { contactName = companyName, city = "Istanbul", country = "Turkey", address = "-" },
                billingAddress = new { contactName = companyName, city = "Istanbul", country = "Turkey", address = "-" },
            },
        };

        var response = await SendAsync<CheckoutFormInitializeResponse>(path, body, cancellationToken);
        return new IyzicoCheckoutFormResult(response?.CheckoutFormContent ?? string.Empty, response?.Token ?? string.Empty);
    }

    public async Task<IyzicoSubscriptionResult> RetrieveCheckoutFormResultAsync(string token, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        // DOĞRULA: bkz. InitializeCheckoutFormAsync üzerindeki not.
        var path = $"/v2/subscription/checkoutform/{Uri.EscapeDataString(token)}";
        var response = await SendAsync<SubscriptionResponse>(path, body: null, cancellationToken);
        return ToResult(response);
    }

    public async Task<IyzicoSubscriptionResult> UpgradeSubscriptionAsync(
        string subscriptionReferenceCode, CompanyPlan newPlan,
        IyzicoUpgradePeriod period, CancellationToken cancellationToken)
    {
        var newPlanReferenceCode = EnsurePlanConfigured(newPlan);

        var path = $"/v2/subscription/subscriptions/{Uri.EscapeDataString(subscriptionReferenceCode)}/upgrade";
        var body = new
        {
            newPricingPlanReferenceCode = newPlanReferenceCode,
            upgradePeriod = period == IyzicoUpgradePeriod.Now ? "NOW" : "NEXT_PERIOD",
            useTrial = false,
            resetRecurrenceCount = false,
        };

        var response = await SendAsync<SubscriptionResponse>(path, body, cancellationToken);
        return ToResult(response);
    }

    public async Task<bool> CancelSubscriptionAsync(string subscriptionReferenceCode, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var path = $"/v2/subscription/subscriptions/{Uri.EscapeDataString(subscriptionReferenceCode)}/cancel";
        var response = await SendAsync<StatusResponse>(path, body: null, cancellationToken);
        return string.Equals(response?.Status, "success", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IyzicoSubscriptionResult> RetrieveSubscriptionAsync(
        string subscriptionReferenceCode, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        // DOĞRULA: bkz. InitializeCheckoutFormAsync üzerindeki not - bu da tahmini bir path.
        var path = $"/v2/subscription/subscriptions/{Uri.EscapeDataString(subscriptionReferenceCode)}";
        var response = await SendAsync<SubscriptionResponse>(path, body: null, cancellationToken);
        return ToResult(response);
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "iyzico yapılandırılmamış (IYZICO_API_KEY/IYZICO_SECRET_KEY boş) - faturalama işlemleri devre dışı.");
        }
    }

    private string EnsurePlanConfigured(CompanyPlan plan)
    {
        EnsureConfigured();

        return _options.PlanReferenceCodeFor(plan)
            ?? throw new InvalidOperationException(
                $"{plan} paketi için iyzico Pricing Plan referans kodu yapılandırılmamış (IYZICO_PLAN_{plan.ToString().ToUpperInvariant()}_REF).");
    }

    private async Task<TResponse?> SendAsync<TResponse>(string path, object? body, CancellationToken cancellationToken)
    {
        var requestBody = body is null ? string.Empty : JsonSerializer.Serialize(body, JsonOptions);
        var headers = IyzicoRequestSigner.Sign(_options, path, requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_options.BaseUrl), path));
        request.Headers.Add("Authorization", headers.Authorization);
        request.Headers.Add("x-iyzi-rnd", headers.RandomKey);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "iyzico isteği başarısız: {Path} -> {StatusCode} {Body}", path, (int)response.StatusCode, responseText);
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<TResponse>(responseText, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "iyzico yanıtı ayrıştırılamadı: {Path} {Body}", path, responseText);
            return default;
        }
    }

    private static IyzicoSubscriptionResult ToResult(SubscriptionResponse? response)
    {
        if (response is null || !string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            return new IyzicoSubscriptionResult(false, null, null, null, null, null, response?.ErrorMessage ?? "iyzico isteği başarısız.");
        }

        DateTimeOffset? periodEnd = response.CurrentPeriodEndDate is { } raw && DateTimeOffset.TryParse(raw, out var parsed)
            ? parsed
            : null;

        return new IyzicoSubscriptionResult(
            true,
            response.CustomerReferenceCode,
            response.ReferenceCode,
            periodEnd,
            response.LastPaymentAmount,
            response.LastPaymentReferenceCode,
            null);
    }

    private sealed record CheckoutFormInitializeResponse(
        [property: JsonPropertyName("checkoutFormContent")] string? CheckoutFormContent,
        [property: JsonPropertyName("token")] string? Token,
        [property: JsonPropertyName("status")] string? Status);

    private sealed record StatusResponse([property: JsonPropertyName("status")] string? Status);

    private sealed record SubscriptionResponse(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("errorMessage")] string? ErrorMessage,
        [property: JsonPropertyName("referenceCode")] string? ReferenceCode,
        [property: JsonPropertyName("customerReferenceCode")] string? CustomerReferenceCode,
        [property: JsonPropertyName("currentPeriodEndDate")] string? CurrentPeriodEndDate,
        [property: JsonPropertyName("lastPaymentAmount")] decimal? LastPaymentAmount,
        [property: JsonPropertyName("lastPaymentReferenceCode")] string? LastPaymentReferenceCode);
}
