using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.Billing.HandleIyzicoWebhook;

/// <summary>
/// iyzico Abonelik webhook'unun içeriği. İmza doğrulaması bu komuta gelmeden ÖNCE, API
/// katmanındaki endpoint'te yapılır (bkz. WebhookEndpoints.cs) - buraya ulaşan her istek zaten
/// güvenilir kabul edilir. Yine de payload'daki referans kodlarına göre eşleşen bir
/// CompanySubscription bulunamazsa (ör. eski/yabancı veri) sessizce yok sayılır.
/// </summary>
public sealed record HandleIyzicoWebhookCommand(
    string EventType, string SubscriptionReferenceCode, string OrderReferenceCode, string CustomerReferenceCode)
    : ICommand<Unit>;
