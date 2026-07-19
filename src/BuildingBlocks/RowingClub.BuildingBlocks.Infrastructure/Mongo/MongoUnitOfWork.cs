using System.Text.Json;
using MongoDB.Driver;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Infrastructure.Outbox;

namespace RowingClub.BuildingBlocks.Infrastructure.Mongo;

/// <summary>
/// One MongoDB multi-document transaction per <see cref="SaveChangesAsync"/> call: every tracked
/// aggregate is written, then every domain event those aggregates raised is drained into the
/// shared outbox collection, all inside the same session (spec section 21). Requires the
/// connection to point at a replica set - see <see cref="MongoOptions"/>.
/// </summary>
public sealed class MongoUnitOfWork(
    IMongoDatabase database,
    ICurrentTenant currentTenant,
    ICorrelationIdAccessor correlationIdAccessor)
    : IMongoUnitOfWork
{
    private readonly List<Func<IClientSessionHandle, CancellationToken, Task>> _pendingWrites = [];
    private readonly List<IHasDomainEvents> _trackedAggregates = [];
    private readonly HashSet<object> _trackedEntities = new(ReferenceEqualityComparer.Instance);

    public void Track<TEntity>(IMongoCollection<TEntity> collection, TEntity entity)
        where TEntity : AggregateRoot<Guid>
    {
        // A repository tracks an entity every time it's loaded (not just on Add), since there's no
        // EF-style change tracker to catch in-memory mutations automatically. The same instance can
        // easily flow through more than one repository call in one request - track it once, or the
        // second write's captured "original version" goes stale mid-SaveChanges and trips a false
        // ConcurrencyException.
        if (!_trackedEntities.Add(entity))
        {
            return;
        }

        if (entity is ITenantOwned owned && currentTenant.IsSet && owned.ClubId != currentTenant.ClubId)
        {
            throw new DomainException(
                "tenant_mismatch", "Bir kayıt, aktif kulüpten farklı bir ClubId ile yazılmaya çalışıldı.");
        }

        var originalVersion = entity.Version;

        _pendingWrites.Add(async (session, cancellationToken) =>
        {
            entity.IncrementVersion();

            var filter = Builders<TEntity>.Filter.And(
                Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id),
                Builders<TEntity>.Filter.Eq(e => e.Version, originalVersion));

            var result = await collection.ReplaceOneAsync(
                session, filter, entity, new ReplaceOptions { IsUpsert = true }, cancellationToken);

            if (result.IsAcknowledged && result.MatchedCount == 0 && result.UpsertedId is null)
            {
                throw new ConcurrencyException(typeof(TEntity).Name, entity.Id);
            }
        });

        if (entity is IHasDomainEvents hasDomainEvents && !_trackedAggregates.Contains(hasDomainEvents))
        {
            _trackedAggregates.Add(hasDomainEvents);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (_pendingWrites.Count == 0)
        {
            return;
        }

        using var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);

        await session.WithTransactionAsync(
            async (sessionHandle, ct) =>
            {
                foreach (var write in _pendingWrites)
                {
                    await write(sessionHandle, ct);
                }

                var outbox = database.GetCollection<OutboxMessage>(MongoCollectionNames.OutboxMessages);

                foreach (var aggregate in _trackedAggregates)
                {
                    foreach (var domainEvent in aggregate.DomainEvents)
                    {
                        var message = OutboxMessage.Create(
                            domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName!,
                            JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                            correlationIdAccessor.CorrelationId);

                        await outbox.InsertOneAsync(sessionHandle, message, cancellationToken: ct);
                    }

                    aggregate.ClearDomainEvents();
                }

                return true;
            },
            cancellationToken: cancellationToken);

        _pendingWrites.Clear();
        _trackedAggregates.Clear();
        _trackedEntities.Clear();
    }
}
