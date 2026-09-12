using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Platform;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class PlatformActivityLogRepository(RowingClubDbContext context) : IPlatformActivityLogRepository
{
    public Task<List<PlatformActivityLog>> GetPageAsync(
        string? search, DateTimeOffset? cursorAtUtc, Guid? cursorId, int take, CancellationToken cancellationToken)
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

        if (cursorAtUtc is { } atUtc && cursorId is { } id)
            query = query.Where(l => l.AtUtc.CompareTo(atUtc) < 0 || (l.AtUtc == atUtc && l.Id.CompareTo(id) < 0));

        return query
            .OrderByDescending(l => l.AtUtc).ThenByDescending(l => l.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, CancellationToken cancellationToken)
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

        return query.CountAsync(cancellationToken);
    }
}
