using RowingClub.Identity.Application.Email;
using RowingClub.Scheduling.Application.Members;

namespace RowingClub.Api.Tenancy;

/// <summary>
/// Scheduling modülünün üye hoş geldin e-postası soyutlamasını Identity'nin SMTP altyapısına
/// bağlar (kompozisyon yalnızca API katmanında - modüller birbirini tanımaz).
/// </summary>
public sealed class EmailMemberWelcomeSender(IEmailSender emailSender) : IMemberWelcomeEmailSender
{
    public Task SendAsync(
        string email, string fullName, string companyName, string setupToken,
        string subdomain, CancellationToken cancellationToken)
    {
        var setupUrl =
            $"https://{subdomain}.{TenantResolver.BaseDomain}/uye/sifre-olustur" +
            $"?token={Uri.EscapeDataString(setupToken)}&email={Uri.EscapeDataString(email)}";
        var bodyHtml = $"""
            <p>Sayın {EmailTemplate.Encode(fullName)},</p>
            <p><b>{EmailTemplate.Encode(companyName)}</b> kulübüne üye olarak eklendiniz. Üye panelinize giriş yapabilmeniz için önce
            kendi şifrenizi oluşturmanız gerekiyor. Aşağıdaki bağlantı 7 gün geçerlidir:</p>
            """;
        var htmlBody = EmailTemplate.Render("Üye Panelinize Hoş Geldiniz", bodyHtml, "Şifremi Oluştur", setupUrl);

        return emailSender.SendAsync(
            new EmailMessage(email, $"{companyName} - Üye Paneline Hoş Geldiniz", htmlBody), cancellationToken);
    }

    public Task SendPasswordResetAsync(
        string email, string fullName, string companyName, string resetToken,
        string subdomain, CancellationToken cancellationToken)
    {
        var resetUrl =
            $"https://{subdomain}.{TenantResolver.BaseDomain}/uye/sifre-olustur" +
            $"?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";
        var bodyHtml = $"""
            <p>Sayın {EmailTemplate.Encode(fullName)},</p>
            <p><b>{EmailTemplate.Encode(companyName)}</b> üye paneliniz için bir şifre sıfırlama talebi aldık. Aşağıdaki bağlantı
            ile yeni şifrenizi belirleyebilirsiniz. Bu bağlantı 1 saat geçerlidir. Bu talebi siz yapmadıysanız
            bu e-postayı yok sayabilirsiniz.</p>
            """;
        var htmlBody = EmailTemplate.Render("Şifre Sıfırlama", bodyHtml, "Şifremi Sıfırla", resetUrl);

        return emailSender.SendAsync(
            new EmailMessage(email, $"{companyName} - Şifre Sıfırlama", htmlBody), cancellationToken);
    }
}
