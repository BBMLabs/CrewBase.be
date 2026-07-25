using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class RecoveryCodeRepository(RowingClubDbContext context) : IRecoveryCodeRepository
{
    public Task<List<UserRecoveryCode>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        context.Set<UserRecoveryCode>().Where(r => r.UserId == userId).ToListAsync(cancellationToken);

    public Task<int> GetUnusedCountAsync(Guid userId, CancellationToken cancellationToken) =>
        context.Set<UserRecoveryCode>().CountAsync(r => r.UserId == userId && !r.IsUsed, cancellationToken);

    public void AddRange(IEnumerable<UserRecoveryCode> codes) =>
        context.Set<UserRecoveryCode>().AddRange(codes);

    public void Update(UserRecoveryCode code) =>
        context.Set<UserRecoveryCode>().Update(code);
}
