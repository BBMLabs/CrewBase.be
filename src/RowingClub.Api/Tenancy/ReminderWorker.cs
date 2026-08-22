using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Identity.Application.Companies.GetCompanySite;
using RowingClub.Scheduling.Application.Reminders;

namespace RowingClub.Api.Tenancy;

/// <summary>
/// Randevu hatırlatmalarını gönderen arka plan servisi. Periyodik olarak tüm aktif firmaları
/// gezer; her firma için AYRI bir DI scope açıp o firmanın tenant veritabanını çözer ve zamanı
/// gelmiş hatırlatmaları e-postayla gönderir. Kaç dakika önce hatırlatılacağı firmanın kendi
/// ayarlarından/üyenin seçiminden gelir.
/// </summary>
public sealed class ReminderWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ReminderWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchAllAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Hatırlatma turu başarısız oldu; bir sonraki turda tekrar denenecek.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                break;
        }
    }

    private async Task DispatchAllAsync(CancellationToken cancellationToken)
    {
        List<CompanySiteDto> companies;
        await using (var listScope = scopeFactory.CreateAsyncScope())
        {
            var sender = listScope.ServiceProvider.GetRequiredService<ISender>();
            companies = await sender.Send(new GetActiveCompanySitesQuery(), cancellationToken);
        }

        foreach (var company in companies)
        {
            // Scope başına tek tenant: TenantDbContext scoped olduğundan firma değiştirmek için
            // her firmaya yeni scope açılır.
            await using var scope = scopeFactory.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<ITenantDatabase>()
                .Set(company.CompanyId, company.DatabaseName, company.Subdomain);

            try
            {
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var sent = await sender.Send(new SendDueRemindersCommand(company.Name), cancellationToken);
                if (sent > 0)
                    logger.LogInformation("{Company} için {Count} hatırlatma gönderildi.", company.Name, sent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Company} için hatırlatmalar gönderilemedi.", company.Name);
            }
        }
    }
}
