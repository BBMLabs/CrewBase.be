using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using RowingClub.Identity.Application.Email;

namespace RowingClub.Identity.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = "noreply@rowingclub.dev";
    public string FromName { get; init; } = "RowingClub";
    public bool UseSsl { get; init; } = false;
}

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mime.To.Add(new MailboxAddress(message.To, message.To));
        mime.Subject = message.Subject;
        var textPart = new TextPart("html") { Text = message.HtmlBody };
        textPart.ContentType.Charset = "utf-8";
        mime.Body = textPart;

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port,
            _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, cancellationToken);

        if (!string.IsNullOrEmpty(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
