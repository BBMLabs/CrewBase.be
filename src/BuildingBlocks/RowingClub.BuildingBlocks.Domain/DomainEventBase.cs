namespace RowingClub.BuildingBlocks.Domain;

public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
