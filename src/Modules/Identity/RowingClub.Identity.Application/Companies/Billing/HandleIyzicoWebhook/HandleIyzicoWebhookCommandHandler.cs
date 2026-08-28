using MediatR;
using Microsoft.Extensions.Logging;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Billing;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing.HandleIyzicoWebhook;

public sealed class HandleIyzicoWebhookCommandHandler(
    ICompanyRepository companyRepository,
    ICompanySubscriptionRepository subscriptionRepository,
    ICompanyPaymentRepository paymentRepository,
    IIyzicoSubscriptionClient iyzicoClient,
    PlanDowngradeApplicator downgradeApplicator,
    IAuditLogger auditLogger,
    ILogger<HandleIyzicoWebhookCommandHandler> logger)
    : IRequestHandler<HandleIyzicoWebhookCommand, Unit>
{
    private const string EventSuccess = "subscription.order.success";
    private const string EventFailure = "subscription.order.failure";

    public async Task<Unit> Handle(HandleIyzicoWebhookCommand request, CancellationToken cancellationToken)
    {
        // Tekrarlanan (replay) webhook - aynı ödeme daha önce işlendiyse hiçbir şey yapma.
        if (await paymentRepository.ExistsByIyzicoPaymentReferenceCodeAsync(request.OrderReferenceCode, cancellationToken))
        {
            return Unit.Value;
        }

        var subscription = await subscriptionRepository.GetByIyzicoSubscriptionReferenceCodeAsync(
            request.SubscriptionReferenceCode, cancellationToken);
        if (subscription is null)
        {
            logger.LogWarning(
                "iyzico webhook: bilinmeyen abonelik referansı {SubscriptionReferenceCode}", request.SubscriptionReferenceCode);
            return Unit.Value;
        }

        var company = await companyRepository.GetByIdAsync(subscription.CompanyId, cancellationToken);
        if (company is null)
        {
            logger.LogWarning("iyzico webhook: firma bulunamadı {CompanyId}", subscription.CompanyId);
            return Unit.Value;
        }

        if (request.EventType == EventFailure)
        {
            paymentRepository.Add(CompanyPayment.Failed(
                company.Id, company.Plan, CompanyPlanPricingCatalog.MonthlyPriceFor(company.Plan), "TRY",
                CompanyPaymentKind.SubscriptionRenewal, request.OrderReferenceCode,
                "Otomatik yenileme tahsilatı başarısız oldu."));

            subscription.MarkPastDue();
            subscriptionRepository.Update(subscription);

            auditLogger.Log("COMPANY_SUBSCRIPTION_PAYMENT_FAILED", company.Id.ToString(), request.OrderReferenceCode);
            return Unit.Value;
        }

        if (request.EventType != EventSuccess)
        {
            logger.LogWarning("iyzico webhook: bilinmeyen olay türü {EventType}", request.EventType);
            return Unit.Value;
        }

        var detail = await iyzicoClient.RetrieveSubscriptionAsync(request.SubscriptionReferenceCode, cancellationToken);
        var newPeriodEnd = detail.CurrentPeriodEndUtc ?? (subscription.CurrentPeriodEndUtc ?? DateTimeOffset.UtcNow).AddMonths(1);

        paymentRepository.Add(CompanyPayment.Succeeded(
            company.Id, company.Plan, detail.ChargedAmount ?? CompanyPlanPricingCatalog.MonthlyPriceFor(company.Plan),
            "TRY", CompanyPaymentKind.SubscriptionRenewal, request.OrderReferenceCode));

        subscription.RecordRenewal(newPeriodEnd);

        // Normalde bekleyen düşürmeler güvenlik-ağı worker'ı tarafından dönem bitmeden ÖNCE
        // iyzico'ya bildirilip uygulanmış olur (bkz. SubscriptionSafetyNetWorker) - worker bir
        // şekilde atlarsa (kesinti vb.) burası son çare olarak yakalar. Bu durumda bu döngü için
        // eski (yüksek) plan fiyatı zaten tahsil edilmiş olabilir - bilinen, kabul edilmiş bir
        // gecikme senaryosu, sessizce düzeltilir ve loglanır.
        if (subscription.PendingPlan is { } pendingPlan && subscription.PendingPlanEffectiveAtUtc <= DateTimeOffset.UtcNow)
        {
            logger.LogWarning(
                "Bekleyen paket düşürme güvenlik-ağı worker'ından önce webhook'ta uygulandı: {CompanyId} -> {PendingPlan}",
                company.Id, pendingPlan);

            await downgradeApplicator.ApplyAsync(company, subscription, cancellationToken);
            companyRepository.Update(company);
        }

        subscriptionRepository.Update(subscription);
        auditLogger.Log("COMPANY_SUBSCRIPTION_RENEWED", company.Id.ToString(), request.OrderReferenceCode);

        return Unit.Value;
    }
}
