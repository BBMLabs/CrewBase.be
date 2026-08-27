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

/// <summary>
/// Opt-in marker exempting a command from <see cref="RowingClub.BuildingBlocks.Application.Behaviors.BlockedIpCheckBehavior{TRequest,TResponse}"/>.
/// Only the IP-blocklist management commands themselves implement this, so a company admin can
/// never lock themselves out by blocking the IP they're currently issuing requests from.
/// </summary>
public interface IBypassIpBlockCheck;
