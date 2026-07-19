using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace RowingClub.BuildingBlocks.Infrastructure.Mongo.Migrations;

/// <summary>Applied once at startup (see Bootstrapper) - safe to run on every deploy, already-applied
/// versions are skipped via the "schema_migrations" collection.</summary>
public sealed class MongoMigrationRunner(IMongoDatabase database, ILogger<MongoMigrationRunner> logger)
{
    public async Task RunAsync(IEnumerable<IMongoMigration> migrations, CancellationToken cancellationToken)
    {
        var appliedCollection = database.GetCollection<AppliedMigration>(MongoCollectionNames.SchemaMigrations);

        var applied = await appliedCollection
            .Find(FilterDefinition<AppliedMigration>.Empty)
            .ToListAsync(cancellationToken);
        var appliedVersions = applied.Select(a => a.Version).ToHashSet();

        foreach (var migration in migrations.OrderBy(m => m.Version))
        {
            if (appliedVersions.Contains(migration.Version))
            {
                continue;
            }

            logger.LogInformation(
                "Mongo migration uygulanıyor: v{Version} - {Name}", migration.Version, migration.Name);

            await migration.ExecuteAsync(database, cancellationToken);

            await appliedCollection.InsertOneAsync(
                new AppliedMigration
                {
                    Version = migration.Version,
                    Name = migration.Name,
                    AppliedAtUtc = DateTimeOffset.UtcNow,
                },
                cancellationToken: cancellationToken);
        }
    }

    private sealed class AppliedMigration
    {
        public int Version { get; init; }

        public string Name { get; init; } = string.Empty;

        public DateTimeOffset AppliedAtUtc { get; init; }
    }
}
