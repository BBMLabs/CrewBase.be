using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing.RequestPlanDowngrade;

public sealed class RequestPlanDowngradeCommandHandler(
    ICompanyRepository companyRepository,
    ICompanySubscriptionRepository subscriptionRepository,
    IAuditLogger auditLogger)
    : IRequestHandler<RequestPlanDowngradeCommand, RequestPlanDowngradeResult>
{
    public async Task<RequestPlanDowngradeResult> Handle(RequestPlanDowngradeCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        if (!Enum.TryParse<CompanyPlan>(request.Plan, ignoreCase: true, out var targetPlan) || !CompanyPlanLimitsCatalog.IsFixed(targetPlan))
            throw new DomainException("invalid_plan", "Geçersiz paket.");

        var order = CompanyPlanLimitsCatalog.UpgradeOrder;
        var currentIndex = Array.IndexOf(order, company.Plan);
        var targetIndex = Array.IndexOf(order, targetPlan);
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= currentIndex)
            throw new DomainException("not_a_downgrade", "Seçilen paket mevcut paketten daha düşük olmalı.");

        var subscription = await subscriptionRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        if (subscription is not { Status: CompanySubscriptionStatus.Active, CurrentPeriodEndUtc: not null })
            throw new DomainException("no_active_subscription", "Aktif bir ücretli aboneliğiniz yok.");

        CompanyPlanLimitsCatalog.EnsureUsageFits(targetPlan, request.UsedBranches, request.UsedMembers, request.UsedBoats);

        subscription.RequestPendingPlan(targetPlan, subscription.CurrentPeriodEndUtc.Value);
        subscriptionRepository.Update(subscription);

        auditLogger.Log(
            "COMPANY_PLAN_DOWNGRADE_REQUESTED", company.Id.ToString(),
            $"{subscription.CurrentPeriodEndUtc:u} tarihinde {targetPlan} paketine geçecek");

        return new RequestPlanDowngradeResult(targetPlan.ToString(), subscription.CurrentPeriodEndUtc.Value);
    }
}
