namespace RowingClub.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Firmanın kendi tenant veritabanındaki IP engel listesine karşı denetim yapar.
/// <see cref="RowingClub.BuildingBlocks.Application.Behaviors.BlockedIpCheckBehavior{TRequest,TResponse}"/>
/// tarafından, yalnızca <see cref="ITenantDatabase.IsSet"/> iken, her komuttan ÖNCE çağrılır.
/// </summary>
public interface IBlockedIpChecker
{
    Task<bool> IsBlockedAsync(string ipAddress, CancellationToken cancellationToken);
}
