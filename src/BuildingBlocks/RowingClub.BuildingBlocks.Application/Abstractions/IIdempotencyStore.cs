namespace RowingClub.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Backs <see cref="Behaviors.IdempotencyBehavior{TRequest,TResponse}"/>. Implemented against
/// Redis in Infrastructure (spec section 3 - "Redis: Idempotency").
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>Returns the previously cached response, if this key was already processed.</summary>
    Task<(bool Found, string? SerializedResponse)> TryGetAsync(string key, CancellationToken cancellationToken);

    Task StoreAsync(string key, string serializedResponse, TimeSpan ttl, CancellationToken cancellationToken);
}
