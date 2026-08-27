namespace RowingClub.Scheduling.Application.Members;

/// <summary>
/// Firma panelinden e-postalı eklenen bir üyeye, kendi şifresini oluşturması için tek kullanımlık
/// bir bağlantı gönderir. E-posta gönderimi kompozisyon katmanında (API) Identity'nin SMTP
/// altyapısına bağlanır - Scheduling modülü SMTP/URL detaylarını bilmez, yalnızca ham firma adı,
/// alt alan adı ve ham (hash'lenmemiş) token'ı iletir.
/// </summary>
public interface IMemberWelcomeEmailSender
{
    Task SendAsync(
        string email, string fullName, string companyName, string setupToken,
        string subdomain, CancellationToken cancellationToken);

    /// <summary>Var olan bir üyeye şifresini yeniden oluşturması için bağlantı gönderir.</summary>
    Task SendPasswordResetAsync(
        string email, string fullName, string companyName, string resetToken,
        string subdomain, CancellationToken cancellationToken);
}
