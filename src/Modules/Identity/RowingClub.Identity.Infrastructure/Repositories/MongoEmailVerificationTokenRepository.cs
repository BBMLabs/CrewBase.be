using MongoDB.Driver;
using RowingClub.BuildingBlocks.Infrastructure.Mongo;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Infrastructure.Persistence;

namespace RowingClub.Identity.Infrastructure.Repositories;

public sealed class MongoEmailVerificationTokenRepository(IMongoDatabase database, IMongoUnitOfWork unitOfWork)
    : IEmailVerificationTokenRepository
{
    private IMongoCollection<EmailVerificationToken> Collection =>
        database.GetCollection<EmailVerificationToken>(IdentityMongoCollectionNames.EmailVerificationTokens);

    public async Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var token = await Collection.Find(Builders<EmailVerificationToken>.Filter.Eq(t => t.TokenHash, tokenHash))
            .FirstOrDefaultAsync(cancellationToken);
        if (token is not null)
        {
            unitOfWork.Track(Collection, token);
        }

        return token;
    }

    public void Add(EmailVerificationToken token) => unitOfWork.Track(Collection, token);
}
