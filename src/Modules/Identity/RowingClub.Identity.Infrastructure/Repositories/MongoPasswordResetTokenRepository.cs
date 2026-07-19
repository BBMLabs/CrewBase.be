using MongoDB.Driver;
using RowingClub.BuildingBlocks.Infrastructure.Mongo;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Infrastructure.Persistence;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class MongoPasswordResetTokenRepository(IMongoDatabase database, IMongoUnitOfWork unitOfWork)
    : IPasswordResetTokenRepository
{
    private IMongoCollection<PasswordResetToken> Collection =>
        database.GetCollection<PasswordResetToken>(IdentityMongoCollectionNames.PasswordResetTokens);

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var token = await Collection.Find(Builders<PasswordResetToken>.Filter.Eq(t => t.TokenHash, tokenHash))
            .FirstOrDefaultAsync(cancellationToken);
        if (token is not null)
        {
            unitOfWork.Track(Collection, token);
        }

        return token;
    }

    public void Add(PasswordResetToken token) => unitOfWork.Track(Collection, token);
}
