namespace RowingClub.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Firmaya bağlı kullanıcıların (CompanyAdmin/Employee) yaptığı işlemleri firmanın kendi tenant
/// veritabanına kaydeder. <see cref="RowingClub.BuildingBlocks.Application.Behaviors.ActivityLogBehavior{TRequest,TResponse}"/>
/// tarafından her başarılı komuttan sonra, yalnızca <see cref="ITenantDatabase.IsSet"/> iken çağrılır.
/// </summary>
public interface IActivityLogWriter
{
    Task RecordAsync(string action, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
}
