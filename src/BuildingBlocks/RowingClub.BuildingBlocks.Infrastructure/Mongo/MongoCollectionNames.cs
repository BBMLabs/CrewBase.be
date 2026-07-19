namespace RowingClub.BuildingBlocks.Infrastructure.Mongo;

/// <summary>
/// Shared collection names every module writes to in the single "rowingclub" database. Module-
/// owned collections are named "&lt;module&gt;_&lt;aggregate&gt;" (e.g. "identity_users") so a
/// multi-collection, cross-module transaction (spec: randevu + kontenjan + paket hakkı) can still
/// run inside one MongoDB replica set without cross-database transaction overhead.
/// </summary>
public static class MongoCollectionNames
{
    public const string OutboxMessages = "outbox_messages";
    public const string InboxMessages = "inbox_messages";
    public const string SchemaMigrations = "schema_migrations";
}
