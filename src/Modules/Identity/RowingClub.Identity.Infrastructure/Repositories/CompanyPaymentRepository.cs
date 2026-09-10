using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class CompanyPaymentRepository(RowingClubDbContext context) : ICompanyPaymentRepository
{
    public Task<bool> ExistsByIyzicoPaymentReferenceCodeAsync(string referenceCode, CancellationToken cancellationToken) =>
        context.Set<CompanyPayment>().AnyAsync(p => p.IyzicoPaymentReferenceCode == referenceCode, cancellationToken);

    public Task<List<CompanyPayment>> GetPageByCompanyIdAsync(
        Guid companyId, DateTimeOffset? cursorOccurredAtUtc, Guid? cursorId, int take, CancellationToken cancellationToken)
    {
        var query = context.Set<CompanyPayment>().Where(p => p.CompanyId == companyId);

        if (cursorOccurredAtUtc is { } occurredAtUtc && cursorId is { } id)
            query = query.Where(p =>
                p.OccurredAtUtc.CompareTo(occurredAtUtc) < 0 || (p.OccurredAtUtc == occurredAtUtc && p.Id.CompareTo(id) < 0));

        return query
            .OrderByDescending(p => p.OccurredAtUtc).ThenByDescending(p => p.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<List<CompanyPayment>> GetAllSucceededAsync(CancellationToken cancellationToken) =>
        context.Set<CompanyPayment>().Where(p => p.Status == CompanyPaymentStatus.Succeeded).ToListAsync(cancellationToken);

    public Task<List<CompanyPayment>> GetPageAsync(
        CompanyPaymentStatus? status, DateTimeOffset? cursorOccurredAtUtc, Guid? cursorId, int take,
        CancellationToken cancellationToken)
    {
        var query = context.Set<CompanyPayment>().AsQueryable();

        if (status is { } statusFilter)
            query = query.Where(p => p.Status == statusFilter);

        if (cursorOccurredAtUtc is { } occurredAtUtc && cursorId is { } id)
            query = query.Where(p =>
                p.OccurredAtUtc.CompareTo(occurredAtUtc) < 0 || (p.OccurredAtUtc == occurredAtUtc && p.Id.CompareTo(id) < 0));

        return query
            .OrderByDescending(p => p.OccurredAtUtc).ThenByDescending(p => p.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<CompanyPaymentStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken)
    {
        var counts = await context.Set<CompanyPayment>()
            .GroupBy(p => p.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Key, x => x.Count);
    }

    public void Add(CompanyPayment payment) => context.Set<CompanyPayment>().Add(payment);
}
