using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.Billing.ConfirmSubscriptionCheckout;

/// <summary>iyzico checkout formundan dönüldükten sonra çağrılır; ödeme başarılıysa abonelik/paket burada aktifleşir.</summary>
public sealed record ConfirmSubscriptionCheckoutCommand(Guid CompanyId, string Plan, string Token) : ICommand<ConfirmSubscriptionCheckoutResult>;

public sealed record ConfirmSubscriptionCheckoutResult(string Plan, DateTimeOffset? NextPaymentDateUtc);
