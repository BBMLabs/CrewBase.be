namespace RowingClub.BuildingBlocks.Application.Abstractions;

/// <summary>
/// One per module DbContext. The <see cref="Behaviors.TransactionBehavior{TRequest,TResponse}"/>
/// commits it after a command handler completes successfully, so handlers never call
/// <c>SaveChanges</c> themselves (spec section 23, mimari test - "Handler dışından transaction
/// yönetimi yapılmamalıdır" is the mirror rule enforced by ArchitectureTests).
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
