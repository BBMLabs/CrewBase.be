using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Identity.Application.Companies.GetCompanySite;

namespace RowingClub.Api.Tenancy;

/// <summary>
/// Açılışta tüm aktif firmaların tenant veritabanlarını EF migration'larıyla günceller (yoksa
/// oluşturur). Katalog migration'ı önce koşar - hosted service'ler kayıt sırasına göre başlar ve
/// EfMigrationHostedService modül kayıtları sırasında, bu servis ise sonrasında eklenir.
/// </summary>
public sealed class TenantMigrationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<TenantMigrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var provisioner = scope.ServiceProvider.GetRequiredService<ITenantDatabaseProvisioner>();

        var companies = await sender.Send(new GetActiveCompanySitesQuery(), cancellationToken);
        foreach (var company in companies)
        {
            try
            {
                await provisioner.ProvisionAsync(company.DatabaseName, cancellationToken);
            }
            catch (Exception ex)
            {
                // Tek bir bozuk tenant tüm uygulamanın açılışını engellememeli.
                logger.LogError(ex, "{Company} tenant veritabanı migrate edilemedi.", company.Name);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
