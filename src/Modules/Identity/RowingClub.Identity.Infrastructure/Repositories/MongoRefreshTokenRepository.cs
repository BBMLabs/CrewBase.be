using MongoDB.Driver;
using RowingClub.BuildingBlocks.Infrastructure.Mongo;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Infrastructure.Persistence;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class MongoRefreshTokenRepository(IMongoDatabase database, IMongoUnitOfWork unitOfWork)
    : IRefreshTokenRepository
{
    private IMongoCollection<RefreshToken> Collection =>
        database.GetCollection<RefreshToken>(IdentityMongoCollectionNames.RefreshTokens);

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var token = await Collection.Find(Builders<RefreshToken>.Filter.Eq(t => t.TokenHash, tokenHash))
            .FirstOrDefaultAsync(cancellationToken);
        if (token is not null)
        {
            unitOfWork.Track(Collection, token);
        }

        return token;
    }

    public async Task<IReadOnlyCollection<RefreshToken>> GetFamilyAsync(
        Guid familyId, CancellationToken cancellationToken)
    {
        var tokens = await Collection.Find(Builders<RefreshToken>.Filter.Eq(t => t.FamilyId, familyId))
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            unitOfWork.Track(Collection, token);
        }

        return tokens;
    }

    public void Add(RefreshToken refreshToken) => unitOfWork.Track(Collection, refreshToken);
}
