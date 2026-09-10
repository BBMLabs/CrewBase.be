using System.Globalization;
using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Platform;

public sealed record GetCompanySubscriptionQuery(Guid CompanyId, string? Cursor = null, int Limit = 25)
    : IRequest<CompanySubscriptionDetailDto>;

public sealed record CompanySubscriptionDetailDto(
    string Status, DateTimeOffset? CurrentPeriodEndUtc, string? PendingPlan, DateTimeOffset? PendingPlanEffectiveAtUtc,
    KeysetResult<CompanyPaymentDto> Payments);

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

        var limit = Math.Clamp(request.Limit, 1, 200);
        var hasCursor = KeysetCursor.TryDecode(request.Cursor, 1, out var keyParts, out var cursorId);
        var cursorOccurredAtUtc = hasCursor ? DateTimeOffset.Parse(keyParts[0], CultureInfo.InvariantCulture) : (DateTimeOffset?)null;

        var payments = await paymentRepository.GetPageByCompanyIdAsync(
            request.CompanyId, cursorOccurredAtUtc, hasCursor ? cursorId : null, limit + 1, cancellationToken);

        var (page, nextCursor) = KeysetPage.Trim(
            payments, limit, p => p.Id, p => [p.OccurredAtUtc.ToString("o", CultureInfo.InvariantCulture)]);

        var paymentDtos = page
            .Select(p => new CompanyPaymentDto(
                p.Id, p.Plan.ToString(), p.Amount, p.Currency, p.Kind.ToString(), p.Status.ToString(),
                p.OccurredAtUtc, p.FailureReason))
            .ToList();

        return new CompanySubscriptionDetailDto(
            subscription?.Status.ToString() ?? CompanySubscriptionStatus.None.ToString(),
            subscription?.CurrentPeriodEndUtc, subscription?.PendingPlan?.ToString(),
            subscription?.PendingPlanEffectiveAtUtc,
            new KeysetResult<CompanyPaymentDto>(paymentDtos, nextCursor));
    }
}
