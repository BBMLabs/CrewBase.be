using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Infrastructure.Outbox;
using RowingClub.BuildingBlocks.Infrastructure.Postgres.Configurations;

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres;

/// <summary>
/// The platform's single EF Core context. <see cref="SaveChangesAsync(CancellationToken)"/> does
/// three things the old Mongo-based unit of work used to do inside its own transaction, now for
/// free inside EF's own SaveChanges transaction: validates tenant ownership,
/// drains raised domain events into <see cref="OutboxMessages"/>, and bumps each changed
/// aggregate's optimistic-concurrency <see cref="AggregateRoot{TId}.Version"/>. Entity mappings for
/// a module are discovered via <see cref="PersistenceAssemblyMarker"/> - see
/// <see cref="OnModelCreating"/> - so this project never references a module directly.
/// </summary>
public sealed class RowingClubDbContext(
    DbContextOptions<RowingClubDbContext> options,
    ICurrentTenant currentTenant,
    ICorrelationIdAccessor correlationIdAccessor,
    IEnumerable<PersistenceAssemblyMarker> persistenceAssemblyMarkers)
    : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());

        foreach (var marker in persistenceAssemblyMarkers)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(marker.Assembly);
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidateTenantOwnership();
        DrainDomainEventsToOutbox();
        BumpAggregateVersions();

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries[0];
            var id = entry.Entity is Entity<Guid> entityWithId ? (object)entityWithId.Id : "unknown";
            throw new ConcurrencyException(entry.Entity.GetType().Name, id);
        }
    }

    private void ValidateTenantOwnership()
    {
        if (!currentTenant.IsSet)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified &&
                entry.Entity.ClubId != currentTenant.ClubId)
            {
                throw new DomainException(
                    "tenant_mismatch", "Bir kayıt, aktif kulüpten farklı bir ClubId ile yazılmaya çalışıldı.");
            }
        }
    }

    private void DrainDomainEventsToOutbox()
    {
        // Materialize first - ChangeTracker.Entries<T>() enumerates the tracker's live entry set,
        // and OutboxMessages.Add(...) below adds a new tracked entity, which would otherwise
        // invalidate this very enumeration ("Collection was modified; enumeration operation may
        // not execute").
        var entries = ChangeTracker.Entries<IHasDomainEvents>().ToList();

        foreach (var entry in entries)
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            foreach (var domainEvent in entry.Entity.DomainEvents)
            {
                var message = OutboxMessage.Create(
                    domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName!,
                    JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    correlationIdAccessor.CorrelationId);

                OutboxMessages.Add(message);
            }

            entry.Entity.ClearDomainEvents();
        }
    }

    private void BumpAggregateVersions()
    {
        foreach (var entry in ChangeTracker.Entries<AggregateRoot<Guid>>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.IncrementVersion();
            }
        }
    }
}
