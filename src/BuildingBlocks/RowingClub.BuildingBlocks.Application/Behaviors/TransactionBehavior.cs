using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Commits the module's <see cref="IUnitOfWork"/> after a command handler runs. Queries are
/// read-only and skip this entirely - see spec section 23 mimari test rule "Handler dışından
/// transaction yönetimi yapılmamalıdır" (the flip side: handlers never call SaveChanges directly).
/// Changes are saved even when the handler throws an expected business exception (wrong password,
/// domain rule violation, ...): those paths often mutate state on purpose - e.g. incrementing a
/// failed-login counter or revoking a reused refresh token family - and that side effect must
/// survive the throw, not be silently discarded.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICommand<TResponse>)
        {
            return await next();
        }

        try
        {
            var response = await next();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return response;
        }
        catch
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
