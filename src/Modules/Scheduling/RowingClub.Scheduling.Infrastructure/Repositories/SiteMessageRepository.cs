using Microsoft.EntityFrameworkCore;
using RowingClub.Scheduling.Domain.Messages;
using RowingClub.Scheduling.Infrastructure.Persistence;

namespace RowingClub.Scheduling.Infrastructure.Repositories;

public sealed class SiteMessageRepository(TenantDbContext context) : ISiteMessageRepository
{
    public Task<SiteMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.SiteMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<List<SiteMessage>> GetPageAsync(
        DateTimeOffset? cursorAtUtc, Guid? cursorId, int take, CancellationToken cancellationToken)
    {
        var query = context.SiteMessages.AsQueryable();

        if (cursorAtUtc is { } atUtc && cursorId is { } id)
            query = query.Where(m =>
                m.CreatedAtUtc.CompareTo(atUtc) < 0 || (m.CreatedAtUtc == atUtc && m.Id.CompareTo(id) < 0));

        return query
            .OrderByDescending(m => m.CreatedAtUtc).ThenByDescending(m => m.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public void Add(SiteMessage message) => context.SiteMessages.Add(message);
}
