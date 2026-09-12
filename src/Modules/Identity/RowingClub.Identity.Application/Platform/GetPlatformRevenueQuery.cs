using MediatR;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Platform;

public sealed record GetPlatformRevenueQuery : IRequest<PlatformRevenueDto>;

public sealed record PlatformRevenueDto(
    decimal TotalRevenueAllTime, decimal TotalRevenueThisMonth, int TotalSucceededPayments,
    List<CompanyRevenueDto> Companies);

public sealed record CompanyRevenueDto(
    Guid CompanyId, string CompanyName, decimal TotalRevenue, int PaymentCount, DateTimeOffset LastPaymentAtUtc);

public sealed class GetPlatformRevenueQueryHandler(
    ICompanyPaymentRepository paymentRepository, ICompanyRepository companyRepository)
    : IRequestHandler<GetPlatformRevenueQuery, PlatformRevenueDto>
{
    public async Task<PlatformRevenueDto> Handle(GetPlatformRevenueQuery request, CancellationToken cancellationToken)
    {
        var payments = await paymentRepository.GetAllSucceededAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var totalAllTime = payments.Sum(p => p.Amount);
        var totalThisMonth = payments
            .Where(p => p.OccurredAtUtc.Year == now.Year && p.OccurredAtUtc.Month == now.Month)
            .Sum(p => p.Amount);

        var byCompany = payments.GroupBy(p => p.CompanyId).ToList();
        var companyIds = byCompany.Select(g => g.Key).ToList();
        var companies = await companyRepository.GetByIdsAsync(companyIds, cancellationToken);
        var companyNames = companies.ToDictionary(c => c.Id, c => c.Name);

        var companyRevenues = byCompany
            .Select(g => new CompanyRevenueDto(
                g.Key, companyNames.GetValueOrDefault(g.Key, "—"), g.Sum(p => p.Amount), g.Count(),
                g.Max(p => p.OccurredAtUtc)))
            .OrderByDescending(c => c.TotalRevenue)
            .ToList();

        return new PlatformRevenueDto(totalAllTime, totalThisMonth, payments.Count, companyRevenues);
    }
}
