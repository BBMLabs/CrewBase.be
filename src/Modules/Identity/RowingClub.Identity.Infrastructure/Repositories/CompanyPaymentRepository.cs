using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class CompanyPaymentRepository(RowingClubDbContext context) : ICompanyPaymentRepository
{
    public Task<bool> ExistsByIyzicoPaymentReferenceCodeAsync(string referenceCode, CancellationToken cancellationToken) =>
        context.Set<CompanyPayment>().AnyAsync(p => p.IyzicoPaymentReferenceCode == referenceCode, cancellationToken);

    public Task<List<CompanyPayment>> GetByCompanyIdAsync(Guid companyId, int take, CancellationToken cancellationToken) =>
        context.Set<CompanyPayment>()
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(CompanyPayment payment) => context.Set<CompanyPayment>().Add(payment);
}
