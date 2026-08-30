using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Billing;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing.CancelSubscription;

public sealed class CancelCompanySubscriptionCommandHandler(
    ICompanyRepository companyRepository,
    ICompanySubscriptionRepository subscriptionRepository,
    IIyzicoSubscriptionClient iyzicoClient,
    IAuditLogger auditLogger)
    : IRequestHandler<CancelCompanySubscriptionCommand, Unit>
{
    public async Task<Unit> Handle(CancelCompanySubscriptionCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        var subscription = await subscriptionRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        if (subscription is not { Status: CompanySubscriptionStatus.Active or CompanySubscriptionStatus.PastDue })
            throw new DomainException("no_active_subscription", "Aktif bir ücretli aboneliği yok.");

        if (subscription.IyzicoSubscriptionReferenceCode is { } referenceCode)
        {
            var cancelled = await iyzicoClient.CancelSubscriptionAsync(referenceCode, cancellationToken);
            if (!cancelled)
                throw new DomainException("iyzico_cancel_failed", "iyzico aboneliği iptal edilemedi.");
        }

        subscription.MarkCancelled();
        subscriptionRepository.Update(subscription);

        company.SetPlan(CompanyPlan.Mico, null, null, null, null);
        companyRepository.Update(company);

        auditLogger.Log("COMPANY_SUBSCRIPTION_CANCELLED", company.Id.ToString(), "Master admin aboneliği iptal etti");

        return Unit.Value;
    }
}
