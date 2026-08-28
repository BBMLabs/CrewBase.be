using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.Billing.ApplyDuePlanDowngrades;

/// <summary>
/// Dönem sonu geçmiş bekleyen paket düşürmelerini bulup UYGULAR: iyzico'nun kendi aboneliğini
/// (yeni dönem başlamadan önce, doğru fiyattan faturalansın diye) hedef pakete geçirir/iptal eder,
/// sonra Company.Plan'ı günceller. SubscriptionSafetyNetWorker tarafından periyodik çağrılır -
/// bu, düşürmenin uygulanması için birincil mekanizmadır (webhook yalnızca kaçırılan bir
/// durumu son çare olarak yakalar, bkz. HandleIyzicoWebhookCommandHandler).
/// </summary>
public sealed record ApplyDuePlanDowngradesCommand : ICommand<int>;
