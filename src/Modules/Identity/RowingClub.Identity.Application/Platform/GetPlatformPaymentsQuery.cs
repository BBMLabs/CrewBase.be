using System.Globalization;
using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Platform;

public sealed record GetPlatformPaymentsQuery(string? Status = null, string? Cursor = null, int Limit = 25)
    : IRequest<KeysetResult<PlatformPaymentDto>>;

public sealed record PlatformPaymentDto(
    Guid CompanyId, string CompanyName, string Plan, decimal Amount, string Currency,
    string Kind, string Status, DateTimeOffset OccurredAtUtc, string? FailureReason);

public sealed record GetPlatformPaymentStatsQuery : IRequest<PlatformPaymentStatsDto>;

public sealed record PlatformPaymentStatsDto(int Total, int Succeeded, int Failed);

public sealed class GetPlatformPaymentsQueryHandler(
    ICompanyPaymentRepository paymentRepository, ICompanyRepository companyRepository)
    : IRequestHandler<GetPlatformPaymentsQuery, KeysetResult<PlatformPaymentDto>>
{
    public async Task<KeysetResult<PlatformPaymentDto>> Handle(
        GetPlatformPaymentsQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 200);
        var statusFilter = !string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<CompanyPaymentStatus>(request.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : (CompanyPaymentStatus?)null;

        var hasCursor = KeysetCursor.TryDecode(request.Cursor, 1, out var keyParts, out var cursorId);
        var cursorAtUtc = hasCursor ? DateTimeOffset.Parse(keyParts[0], CultureInfo.InvariantCulture) : (DateTimeOffset?)null;

        var payments = await paymentRepository.GetPageAsync(
            statusFilter, cursorAtUtc, hasCursor ? cursorId : null, limit + 1, cancellationToken);

        var companyIds = payments.Select(p => p.CompanyId).Distinct().ToList();
        var companies = await companyRepository.GetByIdsAsync(companyIds, cancellationToken);
        var companyNames = companies.ToDictionary(c => c.Id, c => c.Name);

        var (page, nextCursor) = KeysetPage.Trim(
            payments, limit, p => p.Id, p => [p.OccurredAtUtc.ToString("o", CultureInfo.InvariantCulture)]);

        var dtos = page
            .Select(p => new PlatformPaymentDto(
                p.CompanyId, companyNames.GetValueOrDefault(p.CompanyId, "—"), p.Plan.ToString(), p.Amount, p.Currency,
                p.Kind.ToString(), p.Status.ToString(), p.OccurredAtUtc, p.FailureReason))
            .ToList();

        return new KeysetResult<PlatformPaymentDto>(dtos, nextCursor);
    }
}

public sealed class GetPlatformPaymentStatsQueryHandler(ICompanyPaymentRepository paymentRepository)
    : IRequestHandler<GetPlatformPaymentStatsQuery, PlatformPaymentStatsDto>
{
    public async Task<PlatformPaymentStatsDto> Handle(
        GetPlatformPaymentStatsQuery request, CancellationToken cancellationToken)
    {
        var counts = await paymentRepository.GetStatusCountsAsync(cancellationToken);
        var succeeded = counts.GetValueOrDefault(CompanyPaymentStatus.Succeeded);
        var failed = counts.GetValueOrDefault(CompanyPaymentStatus.Failed);
        return new PlatformPaymentStatsDto(succeeded + failed, succeeded, failed);
    }
}
