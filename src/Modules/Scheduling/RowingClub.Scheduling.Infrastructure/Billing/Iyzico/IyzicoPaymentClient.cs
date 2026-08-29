using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RowingClub.Scheduling.Application.Billing;

namespace RowingClub.Scheduling.Infrastructure.Billing.Iyzico;

/// <summary>
/// iyzico klasik Payment/CheckoutForm API istemcisi (docs.iyzico.com/en/products/checkout-form,
/// 2026-08 itibarıyla doğrulandı). CF-Initialize: POST /payment/iyzipos/checkoutform/initialize/auth/ecom.
/// CF-Retrieve: POST /payment/iyzipos/checkoutform/auth/ecom/detail (gövde: locale, conversationId, token).
/// Firma abonelik akışındaki (Identity, /v2/subscription/*) checkoutFormContent HAM HTML dönerken,
/// bu klasik API'de checkoutFormContent BASE64 KODLU HTML döner - fark dokümanla doğrulandı,
/// aşağıda çözülüp Application katmanına ham HTML olarak verilir (frontend'in mevcut
/// dangerouslySetInnerHTML deseni değişmeden çalışsın diye).
/// </summary>
public sealed class IyzicoPaymentClient(
    HttpClient httpClient, IOptions<PackagePaymentOptions> options, ILogger<IyzicoPaymentClient> logger)
    : IIyzicoPaymentClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PackagePaymentOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public async Task<IyzicoPaymentCheckoutFormResult> InitializeCheckoutFormAsync(
        string conversationId, string basketItemId, string basketItemName, decimal price,
        IyzicoPaymentBuyer buyer, string callbackUrl, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        const string path = "/payment/iyzipos/checkoutform/initialize/auth/ecom";
        var priceText = price.ToString("0.00", CultureInfo.InvariantCulture);
        var fullName = $"{buyer.Name} {buyer.Surname}".Trim();
        var body = new
        {
            locale = "tr",
            conversationId,
            price = priceText,
            paidPrice = priceText,
            currency = "TRY",
            basketId = conversationId,
            paymentGroup = "PRODUCT",
            callbackUrl,
            enabledInstallments = new[] { 1 },
            buyer = new
            {
                id = conversationId,
                name = buyer.Name,
                surname = buyer.Surname,
                gsmNumber = "+905000000000",
                email = buyer.Email,
                identityNumber = buyer.IdentityNumber,
                registrationAddress = buyer.RegistrationAddress,
                ip = buyer.Ip,
                city = buyer.City,
                country = buyer.Country,
            },
            shippingAddress = new
            {
                contactName = fullName,
                city = buyer.City,
                country = buyer.Country,
                address = buyer.RegistrationAddress,
            },
            billingAddress = new
            {
                contactName = fullName,
                city = buyer.City,
                country = buyer.Country,
                address = buyer.RegistrationAddress,
            },
            basketItems = new[]
            {
                new
                {
                    id = basketItemId,
                    name = basketItemName,
                    category1 = "Ders Paketi",
                    itemType = "VIRTUAL",
                    price = priceText,
                },
            },
        };

        var response = await SendAsync<CheckoutFormInitializeResponse>(path, body, cancellationToken);
        if (response is null || !string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            return new IyzicoPaymentCheckoutFormResult(string.Empty, string.Empty);
        }

        return new IyzicoPaymentCheckoutFormResult(
            DecodeCheckoutFormContent(response.CheckoutFormContent), response.Token ?? string.Empty);
    }

    public async Task<IyzicoPaymentResult> RetrieveCheckoutFormResultAsync(string token, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        const string path = "/payment/iyzipos/checkoutform/auth/ecom/detail";
        var body = new { locale = "tr", conversationId = token, token };

        var response = await SendAsync<CheckoutFormRetrieveResponse>(path, body, cancellationToken);
        if (response is null || !string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(response.PaymentStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            return new IyzicoPaymentResult(false, null, null, response?.ErrorMessage ?? "Ödeme tamamlanamadı.");
        }

        return new IyzicoPaymentResult(true, response.PaymentId, response.PaidPrice, null);
    }

    private string DecodeCheckoutFormContent(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return string.Empty;
        }

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "iyzico checkoutFormContent Base64 olarak çözülemedi, ham içerik kullanılıyor.");
            return base64;
        }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "iyzico paket ödemesi yapılandırılmamış (IYZICO_API_KEY/IYZICO_SECRET_KEY boş) - satın alma devre dışı.");
        }
    }

    private async Task<TResponse?> SendAsync<TResponse>(string path, object body, CancellationToken cancellationToken)
    {
        var requestBody = JsonSerializer.Serialize(body, JsonOptions);
        var headers = IyzicoPaymentRequestSigner.Sign(_options, path, requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_options.BaseUrl), path));
        request.Headers.Add("Authorization", headers.Authorization);
        request.Headers.Add("x-iyzi-rnd", headers.RandomKey);
        request.Content = JsonContent.Create(body, options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "iyzico paket ödeme isteği başarısız: {Path} -> {StatusCode} {Body}", path, (int)response.StatusCode, responseText);
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<TResponse>(responseText, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "iyzico paket ödeme yanıtı ayrıştırılamadı: {Path} {Body}", path, responseText);
            return default;
        }
    }

    private sealed record CheckoutFormInitializeResponse(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("checkoutFormContent")] string? CheckoutFormContent,
        [property: JsonPropertyName("token")] string? Token,
        [property: JsonPropertyName("errorMessage")] string? ErrorMessage);

    private sealed record CheckoutFormRetrieveResponse(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("paymentStatus")] string? PaymentStatus,
        [property: JsonPropertyName("paymentId")] string? PaymentId,
        [property: JsonPropertyName("paidPrice")] decimal? PaidPrice,
        [property: JsonPropertyName("errorMessage")] string? ErrorMessage);
}
