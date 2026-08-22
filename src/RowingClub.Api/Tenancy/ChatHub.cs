using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Scheduling.Application.Members;

namespace RowingClub.Api.Tenancy;

/// <summary>
/// Üyeler arası GERÇEK ZAMANLI mesajlaşma hub'ı. İstemci bağlanır ve "message" olayını dinler;
/// gönderme REST üzerinden yapılır (kalıcılık + arkadaşlık denetimi orada), hub yalnızca anlık
/// iletim kanalıdır. Kullanıcı kimliği tenant'a göre ayrıştırılır (companyId:customerId), böylece
/// farklı kulüplerdeki aynı müşteri kimlikleri asla çakışmaz.
/// </summary>
[Authorize(Roles = "Member")]
public sealed class ChatHub : Hub
{
}

/// <summary>SignalR kullanıcı kimliği: {company_id}:{customerId} - tenant izolasyonunun anahtarı.</summary>
public sealed class TenantUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var sub = connection.User?.FindFirst("sub")?.Value;
        var companyId = connection.User?.FindFirst("company_id")?.Value;
        return sub is null || companyId is null ? null : $"{companyId}:{sub}";
    }
}

/// <summary>Scheduling'in IChatNotifier'ını SignalR'a bağlar (kompozisyon yalnızca API'de).</summary>
public sealed class SignalRChatNotifier(
    IHubContext<ChatHub> hubContext,
    ITenantDatabase tenantDatabase) : IChatNotifier
{
    public Task PushMessageAsync(Guid recipientCustomerId, MessageDto message, CancellationToken cancellationToken)
    {
        var userId = $"{tenantDatabase.CompanyId}:{recipientCustomerId}";
        return hubContext.Clients.User(userId).SendAsync("message", message, cancellationToken);
    }
}

/// <summary>OTP e-posta iletimi (SMS sağlayıcısı bağlanana kadar telefon kodları da e-postayla).</summary>
public sealed class EmailOtpSender(RowingClub.Identity.Application.Email.IEmailSender emailSender)
    : IOtpSender
{
    public Task SendAsync(string emailTo, string purposeLabel, string code, CancellationToken cancellationToken) =>
        emailSender.SendAsync(new RowingClub.Identity.Application.Email.EmailMessage(
            emailTo,
            $"{purposeLabel} Kodu",
            $"""
            <p>{purposeLabel} için kodunuz:</p>
            <p style="font-size:32px;font-weight:700;letter-spacing:8px;font-family:monospace">{code}</p>
            <p>Bu kod 10 dakika geçerlidir. İşlemi siz başlatmadıysanız bu e-postayı yok sayın.</p>
            """), cancellationToken);
}
