using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Scheduling.Application.Members;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Application.Reminders;

/// <summary>
/// Süresi dolan üye paketlerini siler (gelecekte kullanan iptal edilmemiş bir randevu varsa bu
/// turda atlanır - bkz. plan mimari karar #1) ve firmanın tanımladığı eşiklere göre (ör. 15/7 gün
/// kala) süre dolmadan hatırlatma gönderir (bkz. plan mimari karar #4). API'deki ReminderWorker
/// bunu SendDueRemindersCommand ile aynı per-tenant döngüde çağırır.
/// </summary>
public sealed record ProcessPackageExpiriesCommand(string CompanyName) : ICommand<ProcessPackageExpiriesResult>;

public sealed record ProcessPackageExpiriesResult(int RemindersSent, int PackagesDeleted);

/// <summary>E-posta gönderimi kompozisyon katmanında (API) Identity'nin SMTP altyapısına bağlanır.</summary>
public interface IPackageExpiryReminderSender
{
    Task SendAsync(
        string email, string customerName, string companyName, string packageName,
        DateTimeOffset expiresAtUtc, int daysRemaining, CancellationToken cancellationToken);
}

public sealed class ProcessPackageExpiriesCommandHandler(
    ISettingsRepository settingsRepository,
    ICustomerPackageRepository customerPackageRepository,
    ICustomerRepository customerRepository,
    IAppointmentRepository appointmentRepository,
    IMemberLogRepository memberLogRepository,
    IPackageExpiryReminderSender reminderSender,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<ProcessPackageExpiriesCommand, ProcessPackageExpiriesResult>
{
    public async Task<ProcessPackageExpiriesResult> Handle(
        ProcessPackageExpiriesCommand request, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken) ?? CompanySettings.Default();
        var thresholds = settings.PackageExpiryReminderDays(); // büyükten küçüğe sıralı

        var packages = (await customerPackageRepository.GetAllAsync(cancellationToken))
            .Where(p => p.ExpiresAtUtc is not null)
            .ToList();

        if (packages.Count == 0)
            return new ProcessPackageExpiriesResult(0, 0);

        var nowUtc = DateTimeOffset.UtcNow;
        var nowLocal = settings.NowLocal();
        var today = DateOnly.FromDateTime(nowLocal);
        var nowTime = TimeOnly.FromDateTime(nowLocal);

        var customers = (await customerRepository.GetAllAsync(cancellationToken))
            .ToDictionary(c => c.Id, c => c);

        var remindersSent = 0;
        var packagesDeleted = 0;

        foreach (var package in packages)
        {
            var expiresAtUtc = package.ExpiresAtUtc!.Value;

            if (expiresAtUtc <= nowUtc)
            {
                if (await appointmentRepository.HasActiveFutureAppointmentsUsingPackageAsync(
                        package.Id, today, nowTime, cancellationToken))
                {
                    continue; // gelecekte bu paketi kullanan bir randevu var - bir sonraki turda tekrar denenir
                }

                memberLogRepository.Add(MemberLog.Record(
                    package.CustomerId, MemberEvents.PackageExpired,
                    $"{package.PackageName} (kalan {package.RemainingSessions} ders)"));
                customerPackageRepository.Remove(package);
                packagesDeleted++;
                continue;
            }

            var daysUntilExpiry = (int)Math.Ceiling((expiresAtUtc - nowUtc).TotalDays);

            foreach (var threshold in thresholds)
            {
                if (daysUntilExpiry > threshold)
                    continue; // henüz bu eşiğe ulaşılmadı, daha küçük bir eşik dene

                if (package.LastReminderDaysBeforeExpiry is { } last && last <= threshold)
                    continue; // bu eşik zaten işlendi, daha küçük bir eşik dene

                if (customers.TryGetValue(package.CustomerId, out var customer) && customer.Email is { } email)
                {
                    await reminderSender.SendAsync(
                        email, customer.FullName, request.CompanyName, package.PackageName,
                        expiresAtUtc, daysUntilExpiry, cancellationToken);
                    remindersSent++;
                }

                package.MarkReminderSent(threshold);
                break; // bu turda yalnızca tek (en kaba, henüz gönderilmemiş) eşik için gönderilir
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ProcessPackageExpiriesResult(remindersSent, packagesDeleted);
    }
}
