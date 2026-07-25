using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class CredentialRepository(RowingClubDbContext context) : ICredentialRepository
{
    public Task<Credential?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        context.Set<Credential>().FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public void Add(Credential credential) => context.Set<Credential>().Add(credential);
}
