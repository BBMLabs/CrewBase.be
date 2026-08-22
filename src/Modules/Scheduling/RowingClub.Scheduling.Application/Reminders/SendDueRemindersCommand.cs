using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Application.Reminders;

/// <summary>
/// Firma saat dilimine göre zamanı gelmiş hatırlatmaları gönderir. API'deki arka plan servisi
/// bunu her aktif firma için periyodik çağırır (tenant DB'si önceden çözülmüş olmalıdır).
/// </summary>
public sealed record SendDueRemindersCommand(string CompanyName) : ICommand<int>;

/// <summary>E-posta gönderimi kompozisyon katmanında (API) Identity'nin SMTP altyapısına bağlanır.</summary>
public interface IAppointmentReminderSender
{
    Task SendAsync(
        string email, string customerName, string companyName,
        DateOnly date, TimeOnly startTime, CancellationToken cancellationToken);
}

public sealed class SendDueRemindersCommandHandler(
    ISettingsRepository settingsRepository,
    IAppointmentRepository appointmentRepository,
    IAppointmentReminderSender reminderSender,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SendDueRemindersCommand, int>
{
    public async Task<int> Handle(SendDueRemindersCommand request, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken) ?? CompanySettings.Default();
        var nowLocal = settings.NowLocal();

        var pending = await appointmentRepository.GetPendingRemindersAsync(
            DateOnly.FromDateTime(nowLocal), cancellationToken);

        var sentCount = 0;
        foreach (var appointment in pending)
        {
            var startsAt = appointment.Date.ToDateTime(appointment.StartTime);

            if (startsAt <= nowLocal)
            {
                // Ders başlamış; artık hatırlatma anlamsız - tekrar taranmasın.
                appointment.MarkReminderSent();
                continue;
            }

            if (startsAt - nowLocal > TimeSpan.FromMinutes(appointment.ReminderMinutes!.Value))
                continue; // henüz erken

            if (appointment.Customer.Email is { } email)
            {
                await reminderSender.SendAsync(
                    email, appointment.Customer.FullName, request.CompanyName,
                    appointment.Date, appointment.StartTime, cancellationToken);
                sentCount++;
            }

            appointment.MarkReminderSent();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return sentCount;
    }
}
