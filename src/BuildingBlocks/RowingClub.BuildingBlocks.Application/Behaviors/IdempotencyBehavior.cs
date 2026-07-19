using System.Text.Json;
using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Replays the cached response for a request already processed under the same
/// <c>Idempotency-Key</c> instead of re-running the handler (spec section 11).
/// Only applies to requests that opt in via <see cref="IIdempotentCommand"/>.
/// </summary>
public sealed class IdempotencyBehavior<TRequest, TResponse>(IIdempotencyStore store)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IIdempotentCommand idempotentCommand)
        {
            return await next();
        }

        var storeKey = $"idempotency:{typeof(TRequest).Name}:{idempotentCommand.IdempotencyKey}";

        var (found, cached) = await store.TryGetAsync(storeKey, cancellationToken);
        if (found && cached is not null)
        {
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        var response = await next();

        await store.StoreAsync(storeKey, JsonSerializer.Serialize(response), CacheTtl, cancellationToken);

        return response;
    }
}
