using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowingClub.BuildingBlocks.Infrastructure.Outbox;

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        // (EventId, ConsumerName) is the natural key - a consumer processes a given event at
        // most once, mirroring the unique compound Mongo index this replaces.
        builder.HasKey(x => new { x.EventId, x.ConsumerName });
    }
}
