using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Identity.Application.Companies.GetCompanySite;
using RowingClub.Scheduling.Application.Reminders;
using RowingClub.Scheduling.Application.Rsvp;

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
            await ResolveRsvpsAsync(company, cancellationToken);

            await using var scope = CreateTenantScope(company);

            try
            {
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var sent = await sender.Send(new SendDueRemindersCommand(company.Name), cancellationToken);
                if (sent > 0)
                    logger.LogInformation("{Company} için {Count} hatırlatma gönderildi.", company.Name, sent);

                var packageResult = await sender.Send(new ProcessPackageExpiriesCommand(company.Name), cancellationToken);
                if (packageResult.RemindersSent > 0 || packageResult.PackagesDeleted > 0)
                {
                    logger.LogInformation(
                        "{Company} için {Reminders} paket hatırlatması gönderildi, {Deleted} paket süresi doldu ve silindi.",
                        company.Name, packageResult.RemindersSent, packageResult.PackagesDeleted);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Company} için hatırlatmalar gönderilemedi.", company.Name);
            }
        }
    }

    private AsyncServiceScope CreateTenantScope(CompanySiteDto company)
    {
        var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ITenantDatabase>()
            .Set(
                company.CompanyId, company.DatabaseName, company.Subdomain, company.Plan,
                company.MaxBranches, company.MaxMembers, company.MaxBoats, company.MaxInstructors,
                company.MaxManagers, company.MaxEmployees, company.CanExportData,
                company.HasAdvancedReports, company.HasAutomaticDuesReminders);
        return scope;
    }

    private async Task ResolveRsvpsAsync(CompanySiteDto company, CancellationToken cancellationToken)
    {
        var companyName = company.Name;
        try
        {
            await using var scope = CreateTenantScope(company);
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new ProcessRsvpDeadlinesCommand(), cancellationToken);
            if (result.Confirmed > 0 || result.Cancelled > 0)
            {
                logger.LogInformation(
                    "{Company} için katılım onayı süresi dolan {Confirmed} randevu onaylandı, {Cancelled} randevu iptal edildi.",
                    companyName, result.Confirmed, result.Cancelled);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Company} için katılım onayları işlenemedi.", companyName);
        }
    }
}
