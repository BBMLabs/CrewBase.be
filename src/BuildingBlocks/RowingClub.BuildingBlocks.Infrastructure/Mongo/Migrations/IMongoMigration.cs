using MongoDB.Driver;

namespace RowingClub.BuildingBlocks.Infrastructure.Mongo.Migrations;

/// <summary>
/// A one-time, idempotent schema change: create a collection with a $jsonSchema validator, build
/// an index, backfill a field. Runs once, tracked by <see cref="Version"/> in the
/// "schema_migrations" collection (spec section 8 - "Versiyonlu migration ve index yönetimi").
/// Never mutate an already-shipped migration - add a new one with the next version instead, the
/// same discipline as EF Core migrations.
/// </summary>
public interface IMongoMigration
{
    int Version { get; }

    string Name { get; }

    Task ExecuteAsync(IMongoDatabase database, CancellationToken cancellationToken);
}
