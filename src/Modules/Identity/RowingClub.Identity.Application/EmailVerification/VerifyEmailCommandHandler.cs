using MediatR;
using Microsoft.Extensions.Configuration;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.EmailVerification;

public sealed class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository verificationTokenRepository,
    IRefreshTokenHasher tokenHasher,
    ICompanyRepository companyRepository,
    IOpaqueTokenGenerator tokenGenerator,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IEmailSender emailSender,
    IAuditLogger auditLogger,
    IConfiguration configuration)
    : IRequestHandler<VerifyEmailCommand, Unit>
{
    public const string BaseDomain = "faturebase.com";

    private const string DefaultPublicAppUrl = "https://faturebase.com";

    /// <summary>İlk parola belirleme bağlantısının ömrü — unutma akışındaki 1 saatten daha geniş bir pencere.</summary>
    private static readonly TimeSpan ActivationTokenLifetime = TimeSpan.FromHours(48);

    public async Task<Unit> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);
        var user = await userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new DomainException("verification_failed", "Doğrulama başarısız.");

        if (user.EmailVerified)
            return Unit.Value;

        var tokenHash = tokenHasher.Hash(request.Token);
        var storedToken = await verificationTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new DomainException("verification_failed", "Doğrulama bağlantısı geçersiz.");

        if (storedToken.UserId != user.Id)
            throw new DomainException("verification_failed", "Doğrulama bağlantısı geçersiz.");

        storedToken.MarkUsed();
        user.VerifyEmail();

        // E-posta yeni doğrulanan bir CompanyAdmin'e aitse - yani bu, kayıt sırasındaki e-posta
        // doğrulama adımıysa - firma artık "aktive edilmeye hazır" sayılır: parola belirleme
        // bağlantısı ANCAK ŞİMDİ, sahipliği doğrulanmış e-postaya gönderilir.
        if (user.Role == UserRole.CompanyAdmin && user.CompanyId is { } companyId)
        {
            var company = await companyRepository.GetByIdAsync(companyId, cancellationToken);
            if (company is not null)
                await SendActivationEmailAsync(company, user, cancellationToken);
        }

        return Unit.Value;
    }

    /// <summary>
    /// Site bilgilerini, yönetici kullanıcı adını ve parola belirleme bağlantısını e-postayla iletir.
    /// Parola GÜVENLİK GEREĞİ hiçbir zaman e-postaya yazılmaz; kullanıcı bağlantıdan kendi parolasını
    /// belirler. E-posta gönderilemezse doğrulama geri alınmaz, sadece loglanır — kullanıcı bağlantıyı
    /// "Şifremi Unuttum" akışından yeniden isteyebilir.
    /// </summary>
    private async Task SendActivationEmailAsync(Company company, User adminUser, CancellationToken cancellationToken)
    {
        var adminEmail = adminUser.Email.Value;
        var rawActivationToken = tokenGenerator.Generate();
        var activationTokenHash = tokenHasher.Hash(rawActivationToken);
        var activationToken = PasswordResetToken.Issue(adminUser.Id, activationTokenHash, ActivationTokenLifetime);
        passwordResetTokenRepository.Add(activationToken);

        var siteUrl = $"https://{company.Subdomain}.{BaseDomain}";
        var publicAppUrl = (configuration["PUBLIC_APP_URL"] ?? DefaultPublicAppUrl).TrimEnd('/');
        var activationLink =
            $"{publicAppUrl}/parola-sifirla?token={Uri.EscapeDataString(rawActivationToken)}&email={Uri.EscapeDataString(adminEmail)}";
        var bodyHtml = $"""
            <p>Firmanız başarıyla oluşturuldu ve hemen kullanıma hazır.</p>
            <p style="margin:16px 0;padding:14px 16px;background:#f4f6f8;border-radius:8px;">
              <b>Randevu siteniz:</b> <a href="{siteUrl}" style="color:#155e75;">{siteUrl}</a><br/>
              <b>Yönetici kullanıcı adınız:</b> {adminEmail}
            </p>
            <p>Giriş yapabilmek için önce parolanızı belirlemeniz gerekiyor. Aşağıdaki bağlantı 48 saat geçerlidir.</p>
            <p>Yönetim panelinizden çalışma saatlerinizi, eğitmenlerinizi, teknelerinizi, ders
            paketlerinizi ve hatırlatma kurallarınızı özelleştirebilirsiniz.</p>
            """;
        var htmlBody = EmailTemplate.Render($"{company.Name} — Hoş Geldiniz!", bodyHtml, "Parolamı Belirle", activationLink);

        try
        {
            await emailSender.SendAsync(
                new EmailMessage(adminEmail, $"{company.Name} - FatureBase hesabınız hazır", htmlBody),
                cancellationToken);
        }
        catch (Exception ex)
        {
            auditLogger.Log("WELCOME_EMAIL_FAILED", adminEmail, $"Hoş geldin e-postası gönderilemedi: {ex.Message}");
        }
    }
}
