using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.Billing.GetSubscriptionStatus;

public sealed record GetSubscriptionStatusQuery(Guid CompanyId) : IQuery<SubscriptionStatusDto>;

public sealed record SubscriptionStatusDto(
    string Status,
    DateTimeOffset? NextPaymentDateUtc,
    string? PendingPlan,
    DateTimeOffset? PendingPlanEffectiveAtUtc);
