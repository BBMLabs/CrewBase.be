namespace RowingClub.BuildingBlocks.Domain;

/// <summary>
/// Raised by the persistence layer when an optimistic-concurrency write filter (id + expected
/// Version) matches nothing - another writer updated the aggregate first. Mapped to HTTP 409 by
/// the global exception handler (spec section 18 - Concurrency -> 409).
/// </summary>
public sealed class ConcurrencyException(string entityName, object id)
    : Exception($"'{entityName}' ({id}) başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
