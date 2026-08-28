using MediatR;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing.GetPaymentHistory;

public sealed class GetPaymentHistoryQueryHandler(ICompanyPaymentRepository paymentRepository)
    : IRequestHandler<GetPaymentHistoryQuery, List<CompanyPaymentDto>>
{
    public async Task<List<CompanyPaymentDto>> Handle(GetPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        var payments = await paymentRepository.GetByCompanyIdAsync(request.CompanyId, request.Take, cancellationToken);

        return payments
            .Select(p => new CompanyPaymentDto(
                p.Plan.ToString(), p.Amount, p.Currency, p.Kind.ToString(), p.Status.ToString(),
                p.OccurredAtUtc, p.FailureReason))
            .ToList();
    }
}
