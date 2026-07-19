using MongoDB.Driver;
using RowingClub.BuildingBlocks.Infrastructure.Mongo;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Infrastructure.Persistence;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class MongoCredentialRepository(IMongoDatabase database, IMongoUnitOfWork unitOfWork)
    : ICredentialRepository
{
    private IMongoCollection<Credential> Collection =>
        database.GetCollection<Credential>(IdentityMongoCollectionNames.Credentials);

    public async Task<Credential?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var credential = await Collection.Find(Builders<Credential>.Filter.Eq(c => c.UserId, userId))
            .FirstOrDefaultAsync(cancellationToken);
        if (credential is not null)
        {
            unitOfWork.Track(Collection, credential);
        }

        return credential;
    }

    public void Add(Credential credential) => unitOfWork.Track(Collection, credential);
}
