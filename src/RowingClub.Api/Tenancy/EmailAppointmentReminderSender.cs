using RowingClub.Identity.Application.Email;
using RowingClub.Scheduling.Application.Reminders;

namespace RowingClub.Api.Tenancy;

/// <summary>
/// Scheduling modülünün hatırlatma soyutlamasını Identity'nin SMTP altyapısına bağlar
/// (kompozisyon yalnızca API katmanında - modüller birbirini tanımaz).
/// </summary>
public sealed class EmailAppointmentReminderSender(IEmailSender emailSender) : IAppointmentReminderSender
{
    public Task SendAsync(
        string email, string customerName, string companyName,
        DateOnly date, TimeOnly startTime, CancellationToken cancellationToken)
    {
        var htmlBody = $"""
            <h3>Randevu Hatırlatması</h3>
            <p>Sayın {customerName},</p>
            <p><b>{companyName}</b> firmasındaki randevunuz yaklaşıyor:</p>
            <p><b>{date:dd.MM.yyyy}</b> günü saat <b>{startTime:HH\:mm}</b></p>
            <p>Görüşmek üzere!</p>
            """;

        return emailSender.SendAsync(
            new EmailMessage(email, $"{companyName} - Randevu Hatırlatması", htmlBody), cancellationToken);
    }
}
