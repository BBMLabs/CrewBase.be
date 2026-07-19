using MongoDB.Bson;
using MongoDB.Driver;
using RowingClub.BuildingBlocks.Infrastructure.Mongo.Migrations;

namespace RowingClub.Identity.Infrastructure.Persistence;

/// <summary>
/// Creates the Identity collections with a $jsonSchema validator and builds their unique/compound
/// indexes (spec section 8 - schema validation, index strategy). Version range 100-199 is
/// reserved for Identity - see <see cref="RowingClub.BuildingBlocks.Infrastructure.Mongo.Migrations.CoreCollectionsMigration"/>.
/// </summary>
public sealed class IdentityCollectionsMigration : IMongoMigration
{
    public int Version => 100;

    public string Name => "identity-collections-schema-and-indexes";

    public async Task ExecuteAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        await CreateValidatedCollectionAsync(
            database,
            IdentityMongoCollectionNames.Users,
            RequiredFieldsSchema(
                "email", "emailVerified", "status", "failedLoginAttemptCount", "createdAtUtc", "version"),
            cancellationToken);

        await CreateValidatedCollectionAsync(
            database,
            IdentityMongoCollectionNames.Credentials,
            RequiredFieldsSchema("userId", "passwordHash", "createdAtUtc", "version"),
            cancellationToken);

        await CreateValidatedCollectionAsync(
            database,
            IdentityMongoCollectionNames.RefreshTokens,
            RequiredFieldsSchema(
                "userId", "familyId", "tokenHash", "createdAtUtc", "expiresAtUtc", "version"),
            cancellationToken);

        await CreateValidatedCollectionAsync(
            database,
            IdentityMongoCollectionNames.UserSessions,
            RequiredFieldsSchema(
                "userId", "refreshTokenFamilyId", "createdAtUtc", "lastSeenAtUtc", "version"),
            cancellationToken);

        await CreateValidatedCollectionAsync(
            database,
            IdentityMongoCollectionNames.EmailVerificationTokens,
            RequiredFieldsSchema("userId", "tokenHash", "expiresAtUtc", "version"),
            cancellationToken);

        await CreateValidatedCollectionAsync(
            database,
            IdentityMongoCollectionNames.PasswordResetTokens,
            RequiredFieldsSchema("userId", "tokenHash", "expiresAtUtc", "version"),
            cancellationToken);

        var users = database.GetCollection<BsonDocument>(IdentityMongoCollectionNames.Users);
        await users.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("email.value"),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        var credentials = database.GetCollection<BsonDocument>(IdentityMongoCollectionNames.Credentials);
        await credentials.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("userId"),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        var refreshTokens = database.GetCollection<BsonDocument>(IdentityMongoCollectionNames.RefreshTokens);
        await refreshTokens.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("tokenHash"),
                    new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("familyId")),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("userId")),
            ],
            cancellationToken);

        var userSessions = database.GetCollection<BsonDocument>(IdentityMongoCollectionNames.UserSessions);
        await userSessions.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("refreshTokenFamilyId"),
                    new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("userId")),
            ],
            cancellationToken);

        var emailVerificationTokens = database.GetCollection<BsonDocument>(
            IdentityMongoCollectionNames.EmailVerificationTokens);
        await emailVerificationTokens.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("tokenHash"),
                    new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("userId")),
            ],
            cancellationToken);

        var passwordResetTokens = database.GetCollection<BsonDocument>(
            IdentityMongoCollectionNames.PasswordResetTokens);
        await passwordResetTokens.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("tokenHash"),
                    new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("userId")),
            ],
            cancellationToken);
    }

    private static async Task CreateValidatedCollectionAsync(
        IMongoDatabase database, string collectionName, BsonDocument schema, CancellationToken cancellationToken)
    {
        var existing = await (await database.ListCollectionNamesAsync(
            new ListCollectionNamesOptions { Filter = new BsonDocument("name", collectionName) },
            cancellationToken)).ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            return;
        }

        await database.CreateCollectionAsync(
            collectionName,
            new CreateCollectionOptions<BsonDocument> { Validator = new BsonDocumentFilterDefinition<BsonDocument>(schema) },
            cancellationToken);
    }

    private static BsonDocument RequiredFieldsSchema(params string[] requiredFields) =>
        new("$jsonSchema", new BsonDocument
        {
            { "bsonType", "object" },
            { "required", new BsonArray(requiredFields) },
        });
}
