using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Platform;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class PlatformActivityLogRepository(RowingClubDbContext context) : IPlatformActivityLogRepository
{
    public async Task<(List<PlatformActivityLog> Items, int TotalCount)> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.Set<PlatformActivityLog>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l =>
                EF.Functions.ILike(l.ActorEmail, $"%{term}%") ||
                EF.Functions.ILike(l.Action, $"%{term}%") ||
                (l.IpAddress != null && EF.Functions.ILike(l.IpAddress, $"%{term}%")));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.AtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
