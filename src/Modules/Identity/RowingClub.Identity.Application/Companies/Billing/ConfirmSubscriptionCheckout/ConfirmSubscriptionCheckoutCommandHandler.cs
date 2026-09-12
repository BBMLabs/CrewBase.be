using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Billing;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing.ConfirmSubscriptionCheckout;

public sealed class ConfirmSubscriptionCheckoutCommandHandler(
    ICompanyRepository companyRepository,
    ICompanySubscriptionRepository subscriptionRepository,
    ICompanyPaymentRepository paymentRepository,
    IIyzicoSubscriptionClient iyzicoClient,
    IAuditLogger auditLogger)
    : IRequestHandler<ConfirmSubscriptionCheckoutCommand, ConfirmSubscriptionCheckoutResult>
{
    public async Task<ConfirmSubscriptionCheckoutResult> Handle(
        ConfirmSubscriptionCheckoutCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        if (!Enum.TryParse<CompanyPlan>(request.Plan, ignoreCase: true, out var plan))
            throw new DomainException("invalid_plan", "Geçersiz paket.");

        var result = await iyzicoClient.RetrieveCheckoutFormResultAsync(request.Token, cancellationToken);
        if (!result.Success || result.SubscriptionReferenceCode is null || result.CustomerReferenceCode is null)
            throw new DomainException("checkout_failed", result.ErrorMessage ?? "Ödeme tamamlanamadı.");

        var subscription = await subscriptionRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        if (subscription is null)
        {
            subscription = CompanySubscription.CreateEmpty(request.CompanyId);
            subscriptionRepository.Add(subscription);
        }

        var periodEnd = result.CurrentPeriodEndUtc ?? DateTimeOffset.UtcNow.AddMonths(1);
        subscription.Activate(result.CustomerReferenceCode, result.SubscriptionReferenceCode, periodEnd);
        subscriptionRepository.Update(subscription);

        // Ücretli bir pakete yeni ödeme yapıldı; limit kontrolü gerekmez (ücretsiz Mico'dan her
        // zaman daha geniş limitlere geçiliyor) - SetPlan doğrudan uygular.
        company.SetPlan(
            plan, customMaxBranches: null, customMaxMembers: null, customMaxBoats: null,
            customMaxInstructors: null);
        companyRepository.Update(company);

        var dedupeKey = result.PaymentReferenceCode ?? $"{result.SubscriptionReferenceCode}:initial";
        if (!await paymentRepository.ExistsByIyzicoPaymentReferenceCodeAsync(dedupeKey, cancellationToken))
        {
            paymentRepository.Add(CompanyPayment.Succeeded(
                company.Id, plan, result.ChargedAmount ?? CompanyPlanPricingCatalog.MonthlyPriceFor(plan),
                "TRY", CompanyPaymentKind.InitialSubscription, dedupeKey));
        }

        auditLogger.Log("COMPANY_SUBSCRIPTION_ACTIVATED", company.Id.ToString(), $"Abonelik başladı: {plan}");

        return new ConfirmSubscriptionCheckoutResult(company.Plan.ToString(), subscription.CurrentPeriodEndUtc);
    }
}
