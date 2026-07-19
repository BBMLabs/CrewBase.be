using MongoDB.Driver;
using RowingClub.BuildingBlocks.Infrastructure.Outbox;

namespace RowingClub.BuildingBlocks.Infrastructure.Mongo.Migrations;

/// <summary>
/// Indexes for the collections every module shares (outbox/inbox). Migration version ranges are
/// reserved per owner so all modules' migrations can be merged into one global, strictly-ordered
/// list at startup without colliding: BuildingBlocks 1-99, Identity 100-199, Clubs 200-299,
/// Memberships 300-399, Scheduling 400-499, Packages 500-599, Notifications 600-699,
/// Reporting 700-799.
/// </summary>
public sealed class CoreCollectionsMigration : IMongoMigration
{
    public int Version => 1;

    public string Name => "core-outbox-inbox-indexes";

    public async Task ExecuteAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var outbox = database.GetCollection<OutboxMessage>(MongoCollectionNames.OutboxMessages);
        await outbox.Indexes.CreateOneAsync(
            new CreateIndexModel<OutboxMessage>(
                Builders<OutboxMessage>.IndexKeys.Ascending(m => m.ProcessedOnUtc)),
            cancellationToken: cancellationToken);

        var inbox = database.GetCollection<InboxMessage>(MongoCollectionNames.InboxMessages);
        await inbox.Indexes.CreateOneAsync(
            new CreateIndexModel<InboxMessage>(
                Builders<InboxMessage>.IndexKeys.Ascending(m => m.EventId).Ascending(m => m.ConsumerName),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);
    }
}
