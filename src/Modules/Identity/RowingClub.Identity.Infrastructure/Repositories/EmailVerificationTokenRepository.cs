using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class EmailVerificationTokenRepository(RowingClubDbContext context) : IEmailVerificationTokenRepository
{
    public Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.Set<EmailVerificationToken>().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public void Add(EmailVerificationToken token) => context.Set<EmailVerificationToken>().Add(token);
}
