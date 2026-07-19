namespace RowingClub.BuildingBlocks.Domain;

/// <summary>
/// Non-generic view of <see cref="AggregateRoot{TId}"/> so infrastructure code (EF Core
/// interceptors) can find every tracked aggregate with pending domain events regardless of its
/// id type, without depending on the closed generic.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
