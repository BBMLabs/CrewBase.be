namespace RowingClub.BuildingBlocks.Domain;

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Optimistic concurrency token (spec section 24 - "Concurrency token kullan"). The persistence
    /// layer reads this at load time, matches on it in the write filter, and bumps it via
    /// <see cref="IncrementVersion"/> right before persisting - a mismatch means someone else wrote
    /// the document first (see docs/ARCHITECTURE.md - MongoDB optimistic concurrency).
    /// </summary>
    public long Version { get; private set; }

    /// <summary>Called by the persistence layer only - never call this from domain logic.</summary>
    public void IncrementVersion() => Version++;

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
