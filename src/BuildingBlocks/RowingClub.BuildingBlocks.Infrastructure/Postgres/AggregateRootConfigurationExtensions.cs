using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres;

public static class AggregateRootConfigurationExtensions
{
    /// <summary>
    /// Every aggregate root shares the same id/optimistic-concurrency shape - <see cref="AggregateRoot{TId}.Version"/>
    /// is the concurrency token EF checks in the UPDATE's WHERE clause (mirrors the Mongo
    /// Id+Version filter <see cref="RowingClub.BuildingBlocks.Domain.ConcurrencyException"/> used to guard).
    /// </summary>
    public static EntityTypeBuilder<TEntity> HasAggregateRootDefaults<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AggregateRoot<Guid>
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Version).IsConcurrencyToken();

        return builder;
    }
}
