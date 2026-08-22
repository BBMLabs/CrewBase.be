using Microsoft.EntityFrameworkCore;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Settings;
using RowingClub.Scheduling.Infrastructure.Persistence;

namespace RowingClub.Scheduling.Infrastructure.Repositories;

public sealed class CustomerPackageRepository(TenantDbContext context) : ICustomerPackageRepository
{
    public Task<List<CustomerPackage>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        context.CustomerPackages.Where(p => p.CustomerId == customerId).ToListAsync(cancellationToken);

    public Task<CustomerPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.CustomerPackages.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<List<CustomerPackage>> GetAllAsync(CancellationToken cancellationToken) =>
        context.CustomerPackages.ToListAsync(cancellationToken);

    public void Add(CustomerPackage customerPackage) => context.CustomerPackages.Add(customerPackage);

    public void Remove(CustomerPackage customerPackage) => context.CustomerPackages.Remove(customerPackage);
}

public sealed class MemberLogRepository(TenantDbContext context) : IMemberLogRepository
{
    public Task<List<MemberLog>> GetByCustomerAsync(Guid customerId, int take, CancellationToken cancellationToken) =>
        context.MemberLogs
            .Where(l => l.CustomerId == customerId)
            .OrderByDescending(l => l.AtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<List<MemberLog>> GetRecentAsync(int take, CancellationToken cancellationToken) =>
        context.MemberLogs
            .OrderByDescending(l => l.AtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(MemberLog log) => context.MemberLogs.Add(log);
}

public sealed class ClosedDateRepository(TenantDbContext context) : IClosedDateRepository
{
    public Task<List<ClosedDate>> GetFromAsync(DateOnly from, CancellationToken cancellationToken) =>
        context.ClosedDates.Where(c => c.Date >= from).ToListAsync(cancellationToken);

    public Task<ClosedDate?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken) =>
        context.ClosedDates.FirstOrDefaultAsync(c => c.Date == date, cancellationToken);

    public Task<bool> IsClosedAsync(DateOnly date, CancellationToken cancellationToken) =>
        context.ClosedDates.AnyAsync(c => c.Date == date, cancellationToken);

    public void Add(ClosedDate closedDate) => context.ClosedDates.Add(closedDate);

    public void Remove(ClosedDate closedDate) => context.ClosedDates.Remove(closedDate);
}
