using MongoDB.Driver;
using RowingClub.BuildingBlocks.Infrastructure.Mongo;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;
using RowingClub.Identity.Infrastructure.Persistence;

namespace RowingClub.Identity.Infrastructure.Repositories;

/// <summary>
/// MongoDB.Driver has no change tracker like EF Core - any entity a handler might mutate must be
/// explicitly registered with <see cref="IMongoUnitOfWork"/> the moment it is loaded (not only on
/// <see cref="Add"/>), or the mutation is silently lost at SaveChanges time.
/// </summary>
public sealed class MongoUserRepository(IMongoDatabase database, IMongoUnitOfWork unitOfWork) : IUserRepository
{
    private IMongoCollection<User> Collection => database.GetCollection<User>(IdentityMongoCollectionNames.Users);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await Collection.Find(Builders<User>.Filter.Eq(u => u.Id, id)).FirstOrDefaultAsync(cancellationToken);
        if (user is not null)
        {
            unitOfWork.Track(Collection, user);
        }

        return user;
    }

    public async Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken)
    {
        var user = await Collection.Find(Builders<User>.Filter.Eq(u => u.Email, email))
            .FirstOrDefaultAsync(cancellationToken);
        if (user is not null)
        {
            unitOfWork.Track(Collection, user);
        }

        return user;
    }

    public Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        Collection.Find(Builders<User>.Filter.Eq(u => u.Email, email)).AnyAsync(cancellationToken);

    public void Add(User user) => unitOfWork.Track(Collection, user);
}
