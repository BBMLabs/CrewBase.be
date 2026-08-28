using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Billing;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.UpgradeCompanyPlan;

public sealed class UpgradeCompanyPlanCommandHandler(
    ICompanyRepository companyRepository,
    ICompanySubscriptionRepository subscriptionRepository,
    ICompanyPaymentRepository paymentRepository,
    IIyzicoSubscriptionClient iyzicoClient,
    IAuditLogger auditLogger)
    : IRequestHandler<UpgradeCompanyPlanCommand, UpgradeCompanyPlanResult>
{
    public async Task<UpgradeCompanyPlanResult> Handle(
        UpgradeCompanyPlanCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        if (!Enum.TryParse<CompanyPlan>(request.Plan, ignoreCase: true, out var newPlan))
            throw new DomainException("invalid_plan", "Geçersiz paket.");

        var order = CompanyPlanLimitsCatalog.UpgradeOrder;
        var currentIndex = Array.IndexOf(order, company.Plan);
        var targetIndex = Array.IndexOf(order, newPlan);
        if (currentIndex < 0 || targetIndex < 0 || targetIndex <= currentIndex)
            throw new DomainException("not_an_upgrade", "Seçilen paket mevcut paketten daha yüksek olmalı.");

        var subscription = await subscriptionRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        if (subscription is not { Status: CompanySubscriptionStatus.Active, IyzicoSubscriptionReferenceCode: not null })
            throw new DomainException("no_active_subscription", "Önce bir pakete abone olmalısınız.");

        if (subscription.PendingPlan is not null)
        {
            throw new DomainException(
                "pending_downgrade_exists",
                "Bekleyen bir paket düşürme talebiniz var; önce onu iptal etmelisiniz.");
        }

        // Ücret çekilmeden ÖNCE doğrula - limit aşımı varsa müşteriden hiç para çekilmez.
        CompanyPlanLimitsCatalog.EnsureUsageFits(newPlan, request.UsedBranches, request.UsedMembers, request.UsedBoats);

        var chargeResult = await iyzicoClient.UpgradeSubscriptionAsync(
            subscription.IyzicoSubscriptionReferenceCode, newPlan, IyzicoUpgradePeriod.Now, cancellationToken);

        if (!chargeResult.Success)
            throw new DomainException("payment_failed", chargeResult.ErrorMessage ?? "Ödeme alınamadı.");

        company.ChangePlan(newPlan, request.UsedBranches, request.UsedMembers, request.UsedBoats);
        companyRepository.Update(company);

        if (chargeResult.CurrentPeriodEndUtc is { } newPeriodEnd)
        {
            subscription.RecordRenewal(newPeriodEnd);
            subscriptionRepository.Update(subscription);
        }

        var chargedAmount = chargeResult.ChargedAmount ?? 0m;
        var dedupeKey = chargeResult.PaymentReferenceCode ?? $"{subscription.IyzicoSubscriptionReferenceCode}:upgrade:{DateTimeOffset.UtcNow:O}";
        if (!await paymentRepository.ExistsByIyzicoPaymentReferenceCodeAsync(dedupeKey, cancellationToken))
        {
            paymentRepository.Add(CompanyPayment.Succeeded(
                company.Id, newPlan, chargedAmount, "TRY", CompanyPaymentKind.Upgrade, dedupeKey));
        }

        auditLogger.Log("COMPANY_PLAN_CHANGED", company.Id.ToString(), $"Firma paketini yükseltti: {newPlan} ({chargedAmount} TRY)");

        var limits = company.PlanLimits;
        return new UpgradeCompanyPlanResult(
            company.Plan.ToString(), limits.MaxBranches, limits.MaxMembers, limits.MaxBoats,
            chargedAmount, subscription.CurrentPeriodEndUtc);
    }
}
