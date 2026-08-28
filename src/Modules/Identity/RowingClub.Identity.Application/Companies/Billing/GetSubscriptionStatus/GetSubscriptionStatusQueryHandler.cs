using MediatR;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing.GetSubscriptionStatus;

public sealed class GetSubscriptionStatusQueryHandler(ICompanySubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetSubscriptionStatusQuery, SubscriptionStatusDto>
{
    public async Task<SubscriptionStatusDto> Handle(GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        if (subscription is null)
            return new SubscriptionStatusDto(CompanySubscriptionStatus.None.ToString(), null, null, null);

        return new SubscriptionStatusDto(
            subscription.Status.ToString(),
            subscription.CurrentPeriodEndUtc,
            subscription.PendingPlan?.ToString(),
            subscription.PendingPlanEffectiveAtUtc);
    }
}
