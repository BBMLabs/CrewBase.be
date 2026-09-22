using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Infrastructure.Repositories;

/// <summary>
/// EF Core's ChangeTracker tracks any entity loaded through <see cref="RowingClubDbContext"/>
/// automatically - unlike the old Mongo repositories, mutations to a loaded entity are picked up
/// at <c>SaveChangesAsync</c> time with no explicit tracking call needed.
/// </summary>
public sealed class UserRepository(RowingClubDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<User>().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        context.Set<User>().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        context.Set<User>().AnyAsync(u => u.Email == email, cancellationToken);

    public Task<List<User>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken) =>
        context.Set<User>().Where(u => u.CompanyId == companyId).ToListAsync(cancellationToken);

    public void Add(User user) => context.Set<User>().Add(user);

    public void Remove(User user) => context.Set<User>().Remove(user);
}
