namespace RowingClub.BuildingBlocks.Domain;

/// <summary>
/// Marker for a domain event raised by an aggregate. Published to the outbox in the same
/// transaction as the state change that raised it (see docs/ARCHITECTURE.md - Outbox akışı).
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAtUtc { get; }
}
