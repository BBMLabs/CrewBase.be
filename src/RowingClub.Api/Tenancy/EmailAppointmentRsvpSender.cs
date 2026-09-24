using RowingClub.Identity.Application.Email;
using RowingClub.Scheduling.Application.Rsvp;

namespace RowingClub.Api.Tenancy;

public sealed class EmailAppointmentRsvpSender(IEmailSender emailSender) : IAppointmentRsvpEmailSender
{
    public Task SendAsync(
        string email, string firstName, string companyName, DateOnly date, TimeOnly startTime,
        string boatClass, string rsvpToken, string subdomain, CancellationToken cancellationToken)
    {
        var rsvpUrl = $"https://{subdomain}.{TenantResolver.BaseDomain}/rsvp?token={Uri.EscapeDataString(rsvpToken)}";
        var bodyHtml = $"""
            <p>Merhaba {EmailTemplate.Encode(firstName)},</p>
            <p><b>{EmailTemplate.Encode(companyName)}</b> kulübündeki randevunuz alındı:</p>
            <p style="margin:16px 0;padding:14px 16px;background:#f4f6f8;border-radius:8px;font-size:17px;">
              <b>{date:dd.MM.yyyy}</b> günü saat <b>{startTime:HH\:mm}</b> · {EmailTemplate.Encode(boatClass)}
            </p>
            <p>Lütfen aşağıdaki bağlantıdan katılım durumunuzu bildirin: <b>Katılıyorum</b> veya <b>Katılamıyorum</b>.</p>
            <p>Yanıtınızı 1 saat içinde istediğiniz kadar değiştirebilirsiniz. Bu süre içinde yanıt vermezseniz
            katılımınız onaylanmış sayılır.</p>
            """;
        var htmlBody = EmailTemplate.Render("Katılımınızı Onaylayın", bodyHtml, "Yanıtımı Bildir", rsvpUrl);

        return emailSender.SendAsync(
            new EmailMessage(email, $"{companyName} - Randevu Katılım Onayı", htmlBody), cancellationToken);
    }
}
