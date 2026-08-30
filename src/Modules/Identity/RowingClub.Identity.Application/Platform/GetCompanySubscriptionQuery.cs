using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Platform;

public sealed record GetCompanySubscriptionQuery(Guid CompanyId, int Page = 1, int PageSize = 25)
    : IRequest<CompanySubscriptionDetailDto>;

public sealed record CompanySubscriptionDetailDto(
    string Status, DateTimeOffset? CurrentPeriodEndUtc, string? PendingPlan, DateTimeOffset? PendingPlanEffectiveAtUtc,
    PagedResult<CompanyPaymentDto> Payments);

public sealed record CompanyPaymentDto(
    Guid Id, string Plan, decimal Amount, string Currency, string Kind, string Status,
    DateTimeOffset OccurredAtUtc, string? FailureReason);

public sealed class GetCompanySubscriptionQueryHandler(
    ICompanyRepository companyRepository,
    ICompanySubscriptionRepository subscriptionRepository,
    ICompanyPaymentRepository paymentRepository)
    : IRequestHandler<GetCompanySubscriptionQuery, CompanySubscriptionDetailDto>
{
    public async Task<CompanySubscriptionDetailDto> Handle(
        GetCompanySubscriptionQuery request, CancellationToken cancellationToken)
    {
        _ = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        var subscription = await subscriptionRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var (payments, totalCount) = await paymentRepository.GetPagedByCompanyIdAsync(
            request.CompanyId, page, pageSize, cancellationToken);

        var paymentDtos = payments
            .Select(p => new CompanyPaymentDto(
                p.Id, p.Plan.ToString(), p.Amount, p.Currency, p.Kind.ToString(), p.Status.ToString(),
                p.OccurredAtUtc, p.FailureReason))
            .ToList();

        return new CompanySubscriptionDetailDto(
            subscription?.Status.ToString() ?? CompanySubscriptionStatus.None.ToString(),
            subscription?.CurrentPeriodEndUtc, subscription?.PendingPlan?.ToString(),
            subscription?.PendingPlanEffectiveAtUtc,
            new PagedResult<CompanyPaymentDto>(paymentDtos, totalCount, page, pageSize));
    }
}
