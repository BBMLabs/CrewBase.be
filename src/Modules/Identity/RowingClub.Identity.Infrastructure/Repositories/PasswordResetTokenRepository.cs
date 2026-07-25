using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository(RowingClubDbContext context) : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.Set<PasswordResetToken>().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public void Add(PasswordResetToken token) => context.Set<PasswordResetToken>().Add(token);
}
