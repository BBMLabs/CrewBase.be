using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Billing;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing;

/// <summary>
/// Bekleyen bir paket düşürmesini gerçekten uygular: önce iyzico'nun kendi aboneliğini hedef
/// pakete geçirir (ya da Mico'ysa iptal eder), sonra Company.Plan'ı günceller. Hem
/// <see cref="ApplyDuePlanDowngrades.ApplyDuePlanDowngradesCommandHandler"/> (birincil yol) hem
/// <see cref="HandleIyzicoWebhook.HandleIyzicoWebhookCommandHandler"/> (son çare) tarafından
/// paylaşılır - iki yerde aynı mantığın ayrı ayrı yazılmasını önler.
/// </summary>
public sealed class PlanDowngradeApplicator(IIyzicoSubscriptionClient iyzicoClient)
{
    public async Task<CompanyPlan> ApplyAsync(
        Company company, CompanySubscription subscription, CancellationToken cancellationToken)
    {
        if (subscription.PendingPlan is not { } pendingPlan)
            throw new DomainException("no_pending_plan", "Bekleyen bir paket değişikliği yok.");

        // iyzico çağrısı ÖNCE yapılır ve başarısız olursa fırlatır - subscription/company
        // üzerinde HİÇBİR mutasyon yapılmadan. Sıra tersine çevrilirse (önce ApplyPendingPlan,
        // sonra iyzico) EF'in change tracker'ı iyzico başarısız olsa bile "PendingPlan temizlendi"
        // durumunu commit edebilir - talep sessizce kaybolur ama ne iyzico'da ne Company.Plan'da
        // gerçekleşmiş olur.
        if (subscription.IyzicoSubscriptionReferenceCode is { } referenceCode)
        {
            if (pendingPlan == CompanyPlan.Mico)
            {
                var cancelled = await iyzicoClient.CancelSubscriptionAsync(referenceCode, cancellationToken);
                if (!cancelled)
                    throw new DomainException("iyzico_cancel_failed", "iyzico aboneliği iptal edilemedi.");
            }
            else
            {
                var upgraded = await iyzicoClient.UpgradeSubscriptionAsync(
                    referenceCode, pendingPlan, IyzicoUpgradePeriod.Now, cancellationToken);
                if (!upgraded.Success)
                    throw new DomainException("iyzico_downgrade_failed", upgraded.ErrorMessage ?? "iyzico paket geçişi başarısız oldu.");
            }
        }

        subscription.ApplyPendingPlan();
        if (pendingPlan == CompanyPlan.Mico)
            subscription.MarkCancelled();

        company.SetPlan(pendingPlan, customMaxBranches: null, customMaxMembers: null, customMaxBoats: null);
        return pendingPlan;
    }
}
