using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Firmanın IP engel listesine giren bir adresten gelen HER komutu (hangi modülden olursa olsun -
/// firma paneli işlemleri, üye girişi/kaydı) daha handler'a ulaşmadan reddeder. Yalnızca tenant
/// çözülmüşken (<see cref="ITenantDatabase.IsSet"/>) çalışır; üye girişi ve firma paneli
/// komutlarının ikisi de bu noktada tenant'ı zaten çözmüş olur (bkz. <see cref="ActivityLogBehavior{TRequest,TResponse}"/>
/// ile aynı gerekçe). IBlockedIpChecker kasıtlı olarak IServiceProvider üzerinden tembel çözülür -
/// aynı sebep: somut implementasyon TenantDbContext'e bağımlıdır.
/// </summary>
public sealed class BlockedIpCheckBehavior<TRequest, TResponse>(
    IServiceProvider serviceProvider,
    ICurrentRequestContext currentRequestContext,
    ITenantDatabase tenantDatabase)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is ICommand<TResponse> && request is not IBypassIpBlockCheck &&
            tenantDatabase.IsSet && currentRequestContext.IpAddress is { } ip)
        {
            var checker = serviceProvider.GetRequiredService<IBlockedIpChecker>();
            if (await checker.IsBlockedAsync(ip, cancellationToken))
                throw new DomainException("ip_blocked", "Bu IP adresinden erişim engellenmiştir.");
        }

        return await next();
    }
}
