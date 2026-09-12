using RowingClub.Identity.Application.Email;
using RowingClub.Scheduling.Application.Messages;

namespace RowingClub.Api.Tenancy;

public sealed class EmailSiteMessageReplySender(IEmailSender emailSender) : ISiteMessageReplySender
{
    public Task SendAsync(
        string email, string fullName, string companyName, string originalMessage, string replyText,
        CancellationToken cancellationToken)
    {
        var bodyHtml = $"""
            <p>Sayın {EmailTemplate.Encode(fullName)},</p>
            <p><b>{EmailTemplate.Encode(companyName)}</b> kulübüne ilettiğiniz mesaja yanıt verildi:</p>
            <p style="color:#666;">"{EmailTemplate.Encode(originalMessage)}"</p>
            <p>{EmailTemplate.Encode(replyText)}</p>
            """;
        var htmlBody = EmailTemplate.Render($"{companyName} - Mesajınıza Yanıt", bodyHtml);

        return emailSender.SendAsync(
            new EmailMessage(email, $"{companyName} - Mesajınıza Yanıt", htmlBody), cancellationToken);
    }
}
