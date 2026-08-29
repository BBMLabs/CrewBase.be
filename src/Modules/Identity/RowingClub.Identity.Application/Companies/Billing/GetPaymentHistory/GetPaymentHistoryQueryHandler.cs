using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing.GetPaymentHistory;

public sealed class GetPaymentHistoryQueryHandler(ICompanyPaymentRepository paymentRepository)
    : IRequestHandler<GetPaymentHistoryQuery, PagedResult<CompanyPaymentDto>>
{
    public async Task<PagedResult<CompanyPaymentDto>> Handle(GetPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var (payments, totalCount) = await paymentRepository.GetPagedByCompanyIdAsync(
            request.CompanyId, page, pageSize, cancellationToken);

        var dtos = payments
            .Select(p => new CompanyPaymentDto(
                p.Plan.ToString(), p.Amount, p.Currency, p.Kind.ToString(), p.Status.ToString(),
                p.OccurredAtUtc, p.FailureReason))
            .ToList();

        return new PagedResult<CompanyPaymentDto>(dtos, totalCount, page, pageSize);
    }
}
