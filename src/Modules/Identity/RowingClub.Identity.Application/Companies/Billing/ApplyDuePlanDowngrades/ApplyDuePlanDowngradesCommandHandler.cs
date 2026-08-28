using MediatR;
using Microsoft.Extensions.Logging;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing.ApplyDuePlanDowngrades;

public sealed class ApplyDuePlanDowngradesCommandHandler(
    ICompanyRepository companyRepository,
    ICompanySubscriptionRepository subscriptionRepository,
    PlanDowngradeApplicator downgradeApplicator,
    IAuditLogger auditLogger,
    ILogger<ApplyDuePlanDowngradesCommandHandler> logger)
    : IRequestHandler<ApplyDuePlanDowngradesCommand, int>
{
    public async Task<int> Handle(ApplyDuePlanDowngradesCommand request, CancellationToken cancellationToken)
    {
        var due = await subscriptionRepository.GetDuePendingPlanChangesAsync(DateTimeOffset.UtcNow, cancellationToken);
        var applied = 0;

        foreach (var subscription in due)
        {
            var company = await companyRepository.GetByIdAsync(subscription.CompanyId, cancellationToken);
            if (company is null)
            {
                logger.LogWarning("Bekleyen paket düşürme: firma bulunamadı {CompanyId}", subscription.CompanyId);
                continue;
            }

            try
            {
                var newPlan = await downgradeApplicator.ApplyAsync(company, subscription, cancellationToken);
                companyRepository.Update(company);
                subscriptionRepository.Update(subscription);

                auditLogger.Log("COMPANY_PLAN_DOWNGRADE_APPLIED", company.Id.ToString(), $"Paket düşürüldü: {newPlan}");
                applied++;
            }
            catch (Exception ex)
            {
                // Bir firmadaki hata diğerlerinin işlenmesini engellemez; bir sonraki turda tekrar denenir
                // (PendingPlan bu firma için hâlâ set - subscription.Update çağrılmadığı için değişmedi).
                logger.LogError(ex, "Bekleyen paket düşürme uygulanamadı: {CompanyId}", subscription.CompanyId);
            }
        }

        return applied;
    }
}
