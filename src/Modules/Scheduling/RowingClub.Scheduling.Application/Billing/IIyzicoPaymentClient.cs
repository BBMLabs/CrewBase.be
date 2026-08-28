namespace RowingClub.Scheduling.Application.Billing;

/// <summary>
/// Üye ders paketi satın alma için tek seferlik ödeme istemcisi soyutlaması (klasik iyzico
/// Payment/CheckoutForm API - firma abonelik faturalamasındaki Abonelik API'sinden AYRI, bkz.
/// plan mimari karar #3).
/// </summary>
public interface IIyzicoPaymentClient
{
    bool IsConfigured { get; }

    Task<IyzicoPaymentCheckoutFormResult> InitializeCheckoutFormAsync(
        string conversationId, string basketItemId, string basketItemName, decimal price,
        IyzicoPaymentBuyer buyer, string callbackUrl, CancellationToken cancellationToken);

    Task<IyzicoPaymentResult> RetrieveCheckoutFormResultAsync(string token, CancellationToken cancellationToken);
}

public sealed record IyzicoPaymentBuyer(
    string Name, string Surname, string Email, string IdentityNumber, string RegistrationAddress,
    string City, string Country, string Ip);

public sealed record IyzicoPaymentCheckoutFormResult(string CheckoutFormContentHtml, string Token);

public sealed record IyzicoPaymentResult(
    bool Success, string? PaymentReferenceCode, decimal? PaidPrice, string? ErrorMessage);
