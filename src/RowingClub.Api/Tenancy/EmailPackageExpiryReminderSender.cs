using RowingClub.Identity.Application.Email;
using RowingClub.Scheduling.Application.Reminders;

namespace RowingClub.Api.Tenancy;

/// <summary>
/// Scheduling modülünün paket süresi hatırlatma soyutlamasını Identity'nin SMTP altyapısına
/// bağlar (kompozisyon yalnızca API katmanında - modüller birbirini tanımaz).
/// </summary>
public sealed class EmailPackageExpiryReminderSender(IEmailSender emailSender) : IPackageExpiryReminderSender
{
    public Task SendAsync(
        string email, string customerName, string companyName, string packageName,
        DateTimeOffset expiresAtUtc, int daysRemaining, CancellationToken cancellationToken)
    {
        var bodyHtml = $"""
            <p>Sayın {customerName},</p>
            <p><b>{companyName}</b> firmasındaki <b>{packageName}</b> paketinizin süresi yaklaşıyor:</p>
            <p style="margin:16px 0;padding:14px 16px;background:#f4f6f8;border-radius:8px;font-size:17px;">
              Son kullanım tarihi <b>{expiresAtUtc:dd.MM.yyyy}</b> ({daysRemaining} gün kaldı)
            </p>
            <p>Dersinizin devam etmesi için paketinizi yenilemeyi unutmayın.</p>
            """;
        var htmlBody = EmailTemplate.Render("Paket Süresi Hatırlatması", bodyHtml);

        return emailSender.SendAsync(
            new EmailMessage(email, $"{companyName} - Paket Süresi Hatırlatması", htmlBody), cancellationToken);
    }
}
