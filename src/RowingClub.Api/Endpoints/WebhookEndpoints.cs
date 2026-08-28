using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RowingClub.Api.Tenancy;
using RowingClub.Identity.Application.Companies.Billing.HandleIyzicoWebhook;
using RowingClub.Identity.Infrastructure.Billing.Iyzico;

namespace RowingClub.Api.Endpoints;

/// <summary>
/// Üçüncü taraf ödeme sağlayıcılarından gelen sunucu-sunucu bildirimleri. Kimlik doğrulaması
/// (JWT/rol) YOK - güvenlik tamamen imza doğrulamasına dayanır (bkz. IyzicoWebhookSignatureVerifier).
/// </summary>
public static class WebhookEndpoints
{
    public sealed record IyzicoSubscriptionWebhookPayload(
        string? IyziEventType, string? SubscriptionReferenceCode, string? OrderReferenceCode, string? CustomerReferenceCode);

    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/webhooks").WithTags("Webhooks");

        group.MapPost("/iyzico/subscription", async (
            [FromBody] IyzicoSubscriptionWebhookPayload payload,
            HttpRequest httpRequest,
            IOptions<IyzicoOptions> iyzicoOptions,
            ISender sender,
            CancellationToken ct) =>
        {
            var config = iyzicoOptions.Value;

            // Fail-closed: sır yapılandırılmamışsa hiçbir webhook kabul edilmez (RECAPTCHA'daki
            // "boşsa atla" deseni burada bir güvenlik açığı olurdu - bkz. plan mimari karar #4).
            if (string.IsNullOrWhiteSpace(config.WebhookSecret) || string.IsNullOrWhiteSpace(config.MerchantId))
            {
                return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Faturalama webhook'u yapılandırılmamış.");
            }

            if (payload.IyziEventType is null || payload.SubscriptionReferenceCode is null
                || payload.OrderReferenceCode is null || payload.CustomerReferenceCode is null)
            {
                return Results.BadRequest();
            }

            var signature = httpRequest.Headers["X-IYZ-SIGNATURE-V3"].ToString();
            var verified = IyzicoWebhookSignatureVerifier.Verify(
                config, payload.IyziEventType, payload.SubscriptionReferenceCode,
                payload.OrderReferenceCode, payload.CustomerReferenceCode, signature);

            if (!verified)
            {
                return Results.Unauthorized();
            }

            await sender.Send(new HandleIyzicoWebhookCommand(
                payload.IyziEventType, payload.SubscriptionReferenceCode,
                payload.OrderReferenceCode, payload.CustomerReferenceCode), ct);

            // iyzico başarılı kabul etmezse aynı bildirimi sonsuza kadar tekrar dener - dolayısıyla
            // burada her zaman 200 döndürülür (işlenemeyen/bilinmeyen abonelik referansı bile
            // handler içinde sessizce günlüklenip yutulur, hataya çevrilmez).
            return Results.Ok();
        }).WithName("IyzicoSubscriptionWebhook");

        // --- iyzico CheckoutForm (klasik Payment API) POST->GET köprüsü -----------------------
        // iyzico, checkout tamamlandığında tarayıcıyı callbackUrl'e bir <form method="POST">
        // otomatik gönderimiyle yönlendirir ve "token" değerini GÖVDEDE taşır (query string'de
        // DEĞİL - docs.iyzico.com CheckoutForm entegrasyon dokümanı ile doğrulandı). Frontend SPA
        // statik barındırmadan sunulduğu için bu ham POST'u yakalayamaz; bu yüzden callbackUrl
        // HER ZAMAN buradaki köprü uçlarına verilir - token'ı (query veya form'dan) çıkarır ve
        // SPA'nın React Router route'una query string GET olarak 302 ile iletir.
        group.MapMethods("/iyzico/checkout-callback/plan", new[] { "GET", "POST" }, async (HttpRequest request) =>
        {
            var token = await ExtractCheckoutTokenAsync(request);
            var target = $"https://{TenantResolver.BaseDomain}/panel/paketim/odeme-sonuc";
            return Results.Redirect(AppendToken(target, token));
        }).WithName("IyzicoPlanCheckoutCallback");

        group.MapMethods("/iyzico/checkout-callback/package", new[] { "GET", "POST" }, async (HttpRequest request) =>
        {
            var token = await ExtractCheckoutTokenAsync(request);
            var tenant = SanitizeSubdomain(request.Query["tenant"]);
            var target = tenant is null
                ? $"https://{TenantResolver.BaseDomain}/uye/paketler/odeme-sonuc"
                : $"https://{tenant}.{TenantResolver.BaseDomain}/uye/paketler/odeme-sonuc";
            return Results.Redirect(AppendToken(target, token));
        }).WithName("IyzicoPackageCheckoutCallback");

        return app;
    }

    private static async Task<string?> ExtractCheckoutTokenAsync(HttpRequest request)
    {
        string? token = request.Query["token"];
        if (string.IsNullOrEmpty(token) && request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            token = form["token"];
        }

        return token;
    }

    private static string AppendToken(string basePath, string? token) =>
        string.IsNullOrEmpty(token) ? basePath : $"{basePath}?token={Uri.EscapeDataString(token)}";

    // Açık yönlendirme (open redirect) riskini önlemek için yalnızca alfasayısal/tire karakterlere
    // izin verilir; sabit hedef yollarla (yukarıda) birleştiği için subdomain dışında hiçbir şey
    // saldırganın kontrolüne bırakılmaz.
    private static string? SanitizeSubdomain(string? subdomain) =>
        !string.IsNullOrWhiteSpace(subdomain) && subdomain.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')
            ? subdomain
            : null;
}
