using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing.CancelPendingDowngrade;

public sealed class CancelPendingDowngradeCommandHandler(
    ICompanySubscriptionRepository subscriptionRepository, IAuditLogger auditLogger)
    : IRequestHandler<CancelPendingDowngradeCommand, Unit>
{
    public async Task<Unit> Handle(CancelPendingDowngradeCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("no_active_subscription", "Aktif bir aboneliğiniz yok.");

        subscription.ClearPendingPlan();
        subscriptionRepository.Update(subscription);

        auditLogger.Log("COMPANY_PLAN_DOWNGRADE_CANCELLED", request.CompanyId.ToString(), "Bekleyen paket düşürme iptal edildi");

        return Unit.Value;
    }
}
