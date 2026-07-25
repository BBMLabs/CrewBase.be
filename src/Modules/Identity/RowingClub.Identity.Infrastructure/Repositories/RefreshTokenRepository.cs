using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(RowingClubDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyCollection<RefreshToken>> GetFamilyAsync(
        Guid familyId, CancellationToken cancellationToken) =>
        await context.Set<RefreshToken>().Where(t => t.FamilyId == familyId).ToListAsync(cancellationToken);

    public void Add(RefreshToken refreshToken) => context.Set<RefreshToken>().Add(refreshToken);
}
