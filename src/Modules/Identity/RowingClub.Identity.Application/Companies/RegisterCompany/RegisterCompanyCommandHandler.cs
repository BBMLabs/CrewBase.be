using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Domain.Companies;
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
    IAuditLogger auditLogger)
    : IRequestHandler<RegisterCompanyCommand, RegisterCompanyResponse>
{
    /// <summary>Mock ana domain: her firmaya {subdomain}.faturebase.com adresi verilmiş gibi davranılır.</summary>
    public const string BaseDomain = "faturebase.com";

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
            request.Phone, request.ContactEmail, request.Address);
        companyRepository.Add(company);

        var adminUser = User.RegisterCompanyAdmin(email, company.Id);
        var credential = Credential.Create(adminUser.Id, passwordHasher.Hash(request.AdminPassword));

        userRepository.Add(adminUser);
        credentialRepository.Add(credential);

        await SendWelcomeEmailAsync(company, adminUser.Email.Value, cancellationToken);

        return new RegisterCompanyResponse(
            company.Id, adminUser.Id, company.Name, adminUser.Email.Value,
            company.Subdomain, $"https://{company.Subdomain}.{BaseDomain}", company.CreatedAtUtc);
    }

    /// <summary>
    /// Site bilgilerini ve yönetici kullanıcı adını e-postayla iletir. Şifre GÜVENLİK GEREĞİ
    /// e-postaya yazılmaz (düz metin şifre iletimi güvenlik açığıdır); kullanıcı kayıtta kendi
    /// belirlediği şifreyle giriş yapar. E-posta gönderilemezse kayıt geri alınmaz, sadece loglanır.
    /// </summary>
    private async Task SendWelcomeEmailAsync(Company company, string adminEmail, CancellationToken cancellationToken)
    {
        var siteUrl = $"https://{company.Subdomain}.{BaseDomain}";
        var htmlBody = $"""
            <h2>{company.Name} - Hoş Geldiniz!</h2>
            <p>Firmanız başarıyla oluşturuldu ve hemen kullanıma hazır.</p>
            <ul>
              <li><b>Randevu siteniz:</b> <a href="{siteUrl}">{siteUrl}</a></li>
              <li><b>Yönetici kullanıcı adınız:</b> {adminEmail}</li>
              <li><b>Şifreniz:</b> Kayıt sırasında belirlediğiniz şifre (güvenlik nedeniyle e-postada yer almaz)</li>
            </ul>
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
