using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class CompanySubscriptionRepository(RowingClubDbContext context) : ICompanySubscriptionRepository
{
    public Task<CompanySubscription?> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken) =>
        context.Set<CompanySubscription>().FirstOrDefaultAsync(s => s.CompanyId == companyId, cancellationToken);

    public Task<CompanySubscription?> GetByIyzicoSubscriptionReferenceCodeAsync(
        string referenceCode, CancellationToken cancellationToken) =>
        context.Set<CompanySubscription>()
            .FirstOrDefaultAsync(s => s.IyzicoSubscriptionReferenceCode == referenceCode, cancellationToken);

    public Task<List<CompanySubscription>> GetDuePendingPlanChangesAsync(
        DateTimeOffset asOfUtc, CancellationToken cancellationToken) =>
        context.Set<CompanySubscription>()
            .Where(s => s.PendingPlan != null && s.PendingPlanEffectiveAtUtc != null && s.PendingPlanEffectiveAtUtc <= asOfUtc)
            .ToListAsync(cancellationToken);

    public void Add(CompanySubscription subscription) => context.Set<CompanySubscription>().Add(subscription);

    public void Update(CompanySubscription subscription) => context.Set<CompanySubscription>().Update(subscription);
}
