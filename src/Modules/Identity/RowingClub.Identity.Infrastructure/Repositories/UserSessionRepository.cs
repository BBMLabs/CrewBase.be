using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class UserSessionRepository(RowingClubDbContext context) : IUserSessionRepository
{
    public Task<UserSession?> GetByRefreshTokenFamilyIdAsync(Guid familyId, CancellationToken cancellationToken) =>
        context.Set<UserSession>().FirstOrDefaultAsync(s => s.RefreshTokenFamilyId == familyId, cancellationToken);

    public async Task<IReadOnlyCollection<UserSession>> GetActiveByUserIdAsync(
        Guid userId, CancellationToken cancellationToken) =>
        await context.Set<UserSession>()
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

    public void Add(UserSession session) => context.Set<UserSession>().Add(session);
}
