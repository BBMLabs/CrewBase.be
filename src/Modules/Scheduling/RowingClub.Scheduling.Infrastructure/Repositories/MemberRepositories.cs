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

    public Task<List<CustomerPackage>> GetExpiringWithinAsync(DateTimeOffset maxExpiresAtUtc, CancellationToken cancellationToken) =>
        context.CustomerPackages
            .Where(p => p.ExpiresAtUtc != null && p.ExpiresAtUtc <= maxExpiresAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByPaymentReferenceCodeAsync(string paymentReferenceCode, CancellationToken cancellationToken) =>
        context.CustomerPackages.AnyAsync(p => p.PaymentReferenceCode == paymentReferenceCode, cancellationToken);

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

public sealed class ActivityLogRepository(TenantDbContext context) : IActivityLogRepository
{
    public Task<List<ActivityLog>> GetPageAsync(
        string? search, DateTimeOffset? cursorAtUtc, Guid? cursorId, int take, CancellationToken cancellationToken)
    {
        var query = context.ActivityLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l =>
                EF.Functions.ILike(l.ActorEmail, $"%{term}%") ||
                EF.Functions.ILike(l.Action, $"%{term}%") ||
                (l.IpAddress != null && EF.Functions.ILike(l.IpAddress, $"%{term}%")));
        }

        if (cursorAtUtc is { } atUtc && cursorId is { } id)
            query = query.Where(l => l.AtUtc.CompareTo(atUtc) < 0 || (l.AtUtc == atUtc && l.Id.CompareTo(id) < 0));

        return query
            .OrderByDescending(l => l.AtUtc).ThenByDescending(l => l.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, CancellationToken cancellationToken)
    {
        var query = context.ActivityLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l =>
                EF.Functions.ILike(l.ActorEmail, $"%{term}%") ||
                EF.Functions.ILike(l.Action, $"%{term}%") ||
                (l.IpAddress != null && EF.Functions.ILike(l.IpAddress, $"%{term}%")));
        }

        return query.CountAsync(cancellationToken);
    }

    public void Add(ActivityLog log) => context.ActivityLogs.Add(log);

    public Task<List<ActivityLog>> GetRecentAsync(int take, CancellationToken cancellationToken) =>
        context.ActivityLogs
            .OrderByDescending(l => l.AtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
}

public sealed class BlockedIpAddressRepository(TenantDbContext context) : IBlockedIpAddressRepository
{
    public Task<BlockedIpAddress?> GetByIpAsync(string ipAddress, CancellationToken cancellationToken) =>
        context.BlockedIpAddresses.FirstOrDefaultAsync(b => b.IpAddress == ipAddress, cancellationToken);

    public Task<List<BlockedIpAddress>> GetAllAsync(CancellationToken cancellationToken) =>
        context.BlockedIpAddresses.OrderByDescending(b => b.BlockedAtUtc).ToListAsync(cancellationToken);

    public void Add(BlockedIpAddress entry) => context.BlockedIpAddresses.Add(entry);

    public void Remove(BlockedIpAddress entry) => context.BlockedIpAddresses.Remove(entry);
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
