using MediatR;
using RowingClub.Identity.Application.Companies.Billing.ApplyDuePlanDowngrades;

namespace RowingClub.Api.Tenancy;

/// <summary>
/// Dönem sonu geçmiş bekleyen paket düşürmelerini uygulayan arka plan servisi. Bu, düşürmenin
/// uygulanması için BİRİNCİL mekanizmadır (webhook yalnızca kaçırılan bir durumu son çare olarak
/// yakalar) - iyzico'nun kendi otomatik yenileme turu gelmeden ÖNCE aboneliği hedef pakete
/// geçirmek/iptal etmek gerekir, yoksa müşteri bir dönem daha eski (yüksek) fiyattan
/// faturalanır. Saatlik çalışır: ReminderWorker'ın dakikalık turuna göre çok daha az sıklıkta
/// olması yeterli (paket geçişleri gün bazında planlanır), ama günlükten (eski plandaki tasarım)
/// daha sık - dönem sonu ile gerçek uygulama arasındaki pencereyi dar tutar. Faturalama/dönem
/// verisi tamamen katalog DB'de olduğundan (tenant DB'ye hiç dokunmaz) ReminderWorker'ın
/// aksine firma başına ayrı scope açmaya gerek yoktur.
/// </summary>
public sealed class SubscriptionSafetyNetWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionSafetyNetWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var applied = await sender.Send(new ApplyDuePlanDowngradesCommand(), stoppingToken);
                if (applied > 0)
                    logger.LogInformation("{Count} bekleyen paket düşürme uygulandı.", applied);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bekleyen paket düşürme turu başarısız oldu; bir sonraki turda tekrar denenecek.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                break;
        }
    }
}
