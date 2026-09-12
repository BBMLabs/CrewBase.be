using System.Globalization;
using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing.GetPaymentHistory;

public sealed class GetPaymentHistoryQueryHandler(ICompanyPaymentRepository paymentRepository)
    : IRequestHandler<GetPaymentHistoryQuery, KeysetResult<CompanyPaymentDto>>
{
    public async Task<KeysetResult<CompanyPaymentDto>> Handle(GetPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 200);
        var hasCursor = KeysetCursor.TryDecode(request.Cursor, 1, out var keyParts, out var cursorId);
        var cursorOccurredAtUtc = hasCursor ? DateTimeOffset.Parse(keyParts[0], CultureInfo.InvariantCulture) : (DateTimeOffset?)null;

        var payments = await paymentRepository.GetPageByCompanyIdAsync(
            request.CompanyId, cursorOccurredAtUtc, hasCursor ? cursorId : null, limit + 1, cancellationToken);

        var (page, nextCursor) = KeysetPage.Trim(
            payments, limit, p => p.Id, p => [p.OccurredAtUtc.ToString("o", CultureInfo.InvariantCulture)]);

        var dtos = page
            .Select(p => new CompanyPaymentDto(
                p.Plan.ToString(), p.Amount, p.Currency, p.Kind.ToString(), p.Status.ToString(),
                p.OccurredAtUtc, p.FailureReason))
            .ToList();

        return new KeysetResult<CompanyPaymentDto>(dtos, nextCursor);
    }
}
