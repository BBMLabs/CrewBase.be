using RowingClub.Identity.Application.Email;
using RowingClub.Scheduling.Application.Members;

namespace RowingClub.Api.Tenancy;

/// <summary>
/// Scheduling modülünün şube-aktarım bildirim e-postası soyutlamasını Identity'nin SMTP
/// altyapısına bağlar (kompozisyon yalnızca API katmanında - modüller birbirini tanımaz).
/// Bkz. <see cref="EmailMemberWelcomeSender"/> için aynı desen.
/// </summary>
public sealed class EmailMemberBranchTransferSender(IEmailSender emailSender) : IMemberBranchTransferEmailSender
{
    public Task SendAsync(
        string email, string fullName, string companyName, string oldBranchName, string newBranchName,
        string subdomain, string newBranchCode, CancellationToken cancellationToken)
    {
        var siteUrl = $"https://{subdomain}.{TenantResolver.BaseDomain}/sube/{Uri.EscapeDataString(newBranchCode)}";
        var bodyHtml = $"""
            <p>Sayın {fullName},</p>
            <p><b>{oldBranchName}</b> şubesi kapatıldığı için üyeliğiniz <b>{newBranchName}</b> şubesine
            aktarıldı. Yeni şubenizin web sitesine aşağıdaki bağlantıdan ulaşabilirsiniz:</p>
            """;
        var htmlBody = EmailTemplate.Render("Şubeniz Değişti", bodyHtml, "Yeni Şube Sitem", siteUrl);

        return emailSender.SendAsync(
            new EmailMessage(email, $"{companyName} - Şubeniz Değişti", htmlBody), cancellationToken);
    }
}
