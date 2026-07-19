using MongoDB.Driver;
using RowingClub.BuildingBlocks.Infrastructure.Mongo;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Infrastructure.Persistence;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class MongoUserSessionRepository(IMongoDatabase database, IMongoUnitOfWork unitOfWork)
    : IUserSessionRepository
{
    private IMongoCollection<UserSession> Collection =>
        database.GetCollection<UserSession>(IdentityMongoCollectionNames.UserSessions);

    public async Task<UserSession?> GetByRefreshTokenFamilyIdAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var session = await Collection
            .Find(Builders<UserSession>.Filter.Eq(s => s.RefreshTokenFamilyId, familyId))
            .FirstOrDefaultAsync(cancellationToken);
        if (session is not null)
        {
            unitOfWork.Track(Collection, session);
        }

        return session;
    }

    public async Task<IReadOnlyCollection<UserSession>> GetActiveByUserIdAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var filter = Builders<UserSession>.Filter.And(
            Builders<UserSession>.Filter.Eq(s => s.UserId, userId),
            Builders<UserSession>.Filter.Eq(s => s.RevokedAtUtc, null));

        var sessions = await Collection.Find(filter).ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            unitOfWork.Track(Collection, session);
        }

        return sessions;
    }

    public void Add(UserSession session) => unitOfWork.Track(Collection, session);
}
