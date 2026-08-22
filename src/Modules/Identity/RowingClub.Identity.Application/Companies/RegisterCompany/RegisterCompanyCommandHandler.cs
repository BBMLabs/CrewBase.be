using MediatR;
using Microsoft.Extensions.Configuration;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.Companies.RegisterCompany;

public sealed class RegisterCompanyCommandHandler(
    ICompanyRepository companyRepository,
    IUserRepository userRepository,
    ICredentialRepository credentialRepository,
    IPasswordHasher passwordHasher,
    ITenantDatabaseProvisioner tenantDatabaseProvisioner,
    IEmailSender emailSender,
    IAuditLogger auditLogger,
    IOpaqueTokenGenerator tokenGenerator,
    IRefreshTokenHasher tokenHasher,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IConfiguration configuration)
    : IRequestHandler<RegisterCompanyCommand, RegisterCompanyResponse>
{
    /// <summary>Mock ana domain: her firmaya {subdomain}.faturebase.com adresi verilmiş gibi davranılır.</summary>
    public const string BaseDomain = "faturebase.com";

    private const string DefaultPublicAppUrl = "https://faturebase.com";

    /// <summary>İlk parola belirleme bağlantısının ömrü — unutma akışındaki 1 saatten daha geniş bir pencere.</summary>
    private static readonly TimeSpan ActivationTokenLifetime = TimeSpan.FromHours(48);

    public async Task<RegisterCompanyResponse> Handle(
        RegisterCompanyCommand request, CancellationToken cancellationToken)
    {
        if (await companyRepository.ExistsByNameAsync(request.CompanyName, cancellationToken))
            throw new DomainException("company_name_taken", "Bu şirket adı zaten kullanılıyor.");

        var email = EmailAddress.Create(request.AdminEmail);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new DomainException("email_already_registered", "Bu e-posta adresi zaten kayıtlı.");

        var subdomain = await ResolveUniqueSubdomainAsync(request.CompanyName, cancellationToken);
        var databaseName = SubdomainSlug.ToDatabaseName(subdomain);

        // Tenant veritabanı, katalog kaydından önce ayrı bir bağlantıda oluşturulur (Postgres'te
        // CREATE DATABASE transaction içinde çalışamaz). Kayıt geri alınırsa boş bir tenant DB
        // artakalabilir; aynı subdomain tekrar denendiğinde yeniden kullanılır.
        await tenantDatabaseProvisioner.ProvisionAsync(databaseName, cancellationToken);

        var company = Company.Register(
            request.CompanyName, subdomain, databaseName,
            request.Phone, request.ContactEmail, request.Address, request.TaxNumber);
        companyRepository.Add(company);

        var adminUser = User.RegisterCompanyAdmin(email, company.Id);

        // Kayıtta parola alınmıyor: kullanıcı hiçbir zaman bilmeyeceği rastgele bir parola ile
        // hesap açılır, gerçek parolasını e-postayla gelen bağlantıdan kendisi belirler (aşağıda
        // ForgotPasswordCommandHandler ile birebir aynı PasswordResetToken mekanizması kullanılır).
        var placeholderPasswordHash = passwordHasher.Hash(tokenGenerator.Generate());
        var credential = Credential.Create(adminUser.Id, placeholderPasswordHash);

        userRepository.Add(adminUser);
        credentialRepository.Add(credential);

        var rawActivationToken = tokenGenerator.Generate();
        var activationTokenHash = tokenHasher.Hash(rawActivationToken);
        var activationToken = PasswordResetToken.Issue(adminUser.Id, activationTokenHash, ActivationTokenLifetime);
        passwordResetTokenRepository.Add(activationToken);

        await SendWelcomeEmailAsync(company, adminUser.Email.Value, rawActivationToken, cancellationToken);

        return new RegisterCompanyResponse(
            company.Id, adminUser.Id, company.Name, adminUser.Email.Value,
            company.Subdomain, $"https://{company.Subdomain}.{BaseDomain}", company.CreatedAtUtc);
    }

    /// <summary>
    /// Site bilgilerini, yönetici kullanıcı adını ve parola belirleme bağlantısını e-postayla iletir.
    /// Parola GÜVENLİK GEREĞİ hiçbir zaman e-postaya yazılmaz; kullanıcı bağlantıdan kendi parolasını
    /// belirler. E-posta gönderilemezse kayıt geri alınmaz, sadece loglanır — kullanıcı bağlantıyı
    /// "Şifremi Unuttum" akışından yeniden isteyebilir.
    /// </summary>
    private async Task SendWelcomeEmailAsync(
        Company company, string adminEmail, string rawActivationToken, CancellationToken cancellationToken)
    {
        var siteUrl = $"https://{company.Subdomain}.{BaseDomain}";
        var publicAppUrl = (configuration["PUBLIC_APP_URL"] ?? DefaultPublicAppUrl).TrimEnd('/');
        var activationLink =
            $"{publicAppUrl}/parola-sifirla?token={Uri.EscapeDataString(rawActivationToken)}&email={Uri.EscapeDataString(adminEmail)}";
        var htmlBody = $"""
            <h2>{company.Name} - Hoş Geldiniz!</h2>
            <p>Firmanız başarıyla oluşturuldu ve hemen kullanıma hazır.</p>
            <ul>
              <li><b>Randevu siteniz:</b> <a href="{siteUrl}">{siteUrl}</a></li>
              <li><b>Yönetici kullanıcı adınız:</b> {adminEmail}</li>
            </ul>
            <p>Giriş yapabilmek için önce parolanızı belirlemeniz gerekiyor:
            <a href="{activationLink}">parolamı belirle</a>. Bu bağlantı 48 saat geçerlidir.</p>
            <p>Yönetim panelinizden çalışma saatlerinizi, eğitmenlerinizi, teknelerinizi, ders
            paketlerinizi ve hatırlatma kurallarınızı özelleştirebilirsiniz.</p>
            """;

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

    private async Task<string> ResolveUniqueSubdomainAsync(string companyName, CancellationToken cancellationToken)
    {
        var baseSlug = SubdomainSlug.FromCompanyName(companyName);
        var candidate = baseSlug;
        var suffix = 2;

        while (await companyRepository.ExistsBySubdomainAsync(candidate, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }
}
