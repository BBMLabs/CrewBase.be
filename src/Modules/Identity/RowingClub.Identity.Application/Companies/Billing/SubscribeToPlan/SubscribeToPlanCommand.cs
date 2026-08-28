using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.Billing.SubscribeToPlan;

/// <summary>
/// İlk ücretli abonelik başlatma (Mico'dan bir ücretli pakete, ya da iptal sonrası yeniden abone
/// olma). iyzico checkout formunu döner - kart bilgisi iyzico'nun formunda girilir, bize hiç
/// ulaşmaz. Firma paketi ve abonelik ancak <see cref="ConfirmSubscriptionCheckout.ConfirmSubscriptionCheckoutCommand"/>
/// ile, kart onaylandıktan sonra aktifleşir.
/// </summary>
public sealed record SubscribeToPlanCommand(Guid CompanyId, string Plan, string CallbackUrl) : ICommand<SubscribeToPlanResult>;

public sealed record SubscribeToPlanResult(string CheckoutFormContent, string Token);
