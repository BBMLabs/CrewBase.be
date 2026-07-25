using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class PendingTwoFactorTokenRepository(RowingClubDbContext context) : IPendingTwoFactorTokenRepository
{
    public Task<PendingTwoFactorToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.Set<PendingTwoFactorToken>().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public void Add(PendingTwoFactorToken token) => context.Set<PendingTwoFactorToken>().Add(token);

    public void Update(PendingTwoFactorToken token) => context.Set<PendingTwoFactorToken>().Update(token);
}
