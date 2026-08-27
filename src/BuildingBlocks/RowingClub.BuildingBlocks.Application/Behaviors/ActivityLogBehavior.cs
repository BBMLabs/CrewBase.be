using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Firma paneline giriş yapmış bir kullanıcının (CompanyAdmin/Employee) her başarılı komutunu
/// firmanın "Log" ekranı için otomatik olarak kaydeder - tek tek handler'a loglama koymaya gerek
/// kalmaz. Sorgular (IQuery) ve tenant'ı henüz çözülmemiş istekler (ör. Master panel işlemleri,
/// login öncesi akışlar, açılıştaki tenant migration taraması) sessizce atlanır. İstek gövdesi
/// ASLA loglanmaz (PII/parola riski) - yalnızca hangi işlemin kim tarafından ne zaman yapıldığı
/// tutulur.
///
/// IActivityLogWriter kasıtlı olarak IServiceProvider üzerinden TEMBEL çözülür: somut
/// implementasyon TenantDbContext'e bağımlıdır ve DbContext'in DI tarafından inşa edilmesi,
/// tenant henüz çözülmemişken bile ITenantDatabase.DatabaseName'i okumaya çalışıp anında
/// patlar. Bunu constructor'a koymak, tenant'la hiç ilgisi olmayan HER request'i (ör. açılıştaki
/// TenantMigrationHostedService taraması) kırar - yalnızca gerçekten loglayacağımız an,
/// IsSet doğrulandıktan SONRA inşa edilmelidir.
/// </summary>
public sealed class ActivityLogBehavior<TRequest, TResponse>(
    IServiceProvider serviceProvider,
    ICurrentUser currentUser,
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
        var response = await next();

        if (request is ICommand<TResponse> && tenantDatabase.IsSet &&
            currentUser.IsAuthenticated && currentUser.Role is "CompanyAdmin" or "Employee")
        {
            var action = typeof(TRequest).Name;
            if (action.EndsWith("Command", StringComparison.Ordinal))
                action = action[..^"Command".Length];

            var activityLogWriter = serviceProvider.GetRequiredService<IActivityLogWriter>();
            await activityLogWriter.RecordAsync(action, currentRequestContext.IpAddress, currentRequestContext.UserAgent, cancellationToken);
        }

        return response;
    }
}
