using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Billing;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing.SubscribeToPlan;

public sealed class SubscribeToPlanCommandHandler(
    ICompanyRepository companyRepository,
    ICompanySubscriptionRepository subscriptionRepository,
    IIyzicoSubscriptionClient iyzicoClient)
    : IRequestHandler<SubscribeToPlanCommand, SubscribeToPlanResult>
{
    public async Task<SubscribeToPlanResult> Handle(SubscribeToPlanCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        if (!Enum.TryParse<CompanyPlan>(request.Plan, ignoreCase: true, out var plan) || !iyzicoClient.SupportsPlan(plan))
            throw new DomainException("invalid_plan", "Geçersiz ya da abonelik için yapılandırılmamış paket.");

        var subscription = await subscriptionRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        if (subscription is { Status: CompanySubscriptionStatus.Active })
            throw new DomainException("already_subscribed", "Zaten aktif bir aboneliğiniz var; yükseltme/düşürme uçlarını kullanın.");

        var result = await iyzicoClient.InitializeCheckoutFormAsync(
            company.Id, company.Name, company.ContactEmail ?? string.Empty, plan, request.CallbackUrl, cancellationToken);

        if (string.IsNullOrWhiteSpace(result.CheckoutFormContent))
            throw new DomainException("checkout_init_failed", "Ödeme formu başlatılamadı.");

        return new SubscribeToPlanResult(result.CheckoutFormContent, result.Token);
    }
}
