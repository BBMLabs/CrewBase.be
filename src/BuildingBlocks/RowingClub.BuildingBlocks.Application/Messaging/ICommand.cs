using MediatR;

namespace RowingClub.BuildingBlocks.Application.Messaging;

/// <summary>A write operation. Wrapped in a unit-of-work transaction by the pipeline.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>A read operation. Never wrapped in a transaction, never mutates state.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;

/// <summary>
/// Opt-in marker for commands that must be safe to retry with the same client-supplied key
/// without double-executing the side effect (spec section 11 - "Idempotency key desteği").
/// </summary>
public interface IIdempotentCommand
{
    string IdempotencyKey { get; }
}
