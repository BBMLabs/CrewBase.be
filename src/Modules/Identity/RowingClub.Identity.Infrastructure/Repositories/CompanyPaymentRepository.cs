using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class CompanyPaymentRepository(RowingClubDbContext context) : ICompanyPaymentRepository
{
    public Task<bool> ExistsByIyzicoPaymentReferenceCodeAsync(string referenceCode, CancellationToken cancellationToken) =>
        context.Set<CompanyPayment>().AnyAsync(p => p.IyzicoPaymentReferenceCode == referenceCode, cancellationToken);

    public async Task<(List<CompanyPayment> Items, int TotalCount)> GetPagedByCompanyIdAsync(
        Guid companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.Set<CompanyPayment>().Where(p => p.CompanyId == companyId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Add(CompanyPayment payment) => context.Set<CompanyPayment>().Add(payment);
}
