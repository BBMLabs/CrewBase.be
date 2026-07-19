using MongoDB.Driver;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.BuildingBlocks.Infrastructure.Mongo;

/// <summary>
/// Registers an aggregate to be written (inserted or replaced) the next time
/// <see cref="IUnitOfWork.SaveChangesAsync"/> runs. Mongo repositories call this instead of
/// writing to their collection directly - mirrors how EF's ChangeTracker defers writes to
/// SaveChanges, but explicit, since MongoDB.Driver has no change tracker of its own.
/// </summary>
public interface IMongoUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// Whole-document replace, upserting if new. The filter matches on <c>Id</c> AND the
    /// <see cref="AggregateRoot{TId}.Version"/> the aggregate was loaded with (or 0, for a brand
    /// new one) - a mismatch at save time means another writer got there first
    /// (<see cref="ConcurrencyException"/>). Also enforces <see cref="ITenantOwned"/> as soon as
    /// the aggregate is tracked, not just at save time.
    /// </summary>
    void Track<TEntity>(IMongoCollection<TEntity> collection, TEntity entity)
        where TEntity : AggregateRoot<Guid>;
}
