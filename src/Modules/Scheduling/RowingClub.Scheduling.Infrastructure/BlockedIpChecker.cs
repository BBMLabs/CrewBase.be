using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Scheduling.Infrastructure.Persistence;

namespace RowingClub.Scheduling.Infrastructure;

public sealed class BlockedIpChecker(TenantDbContext context) : IBlockedIpChecker
{
    public Task<bool> IsBlockedAsync(string ipAddress, CancellationToken cancellationToken) =>
        context.BlockedIpAddresses.AnyAsync(b => b.IpAddress == ipAddress, cancellationToken);
}
