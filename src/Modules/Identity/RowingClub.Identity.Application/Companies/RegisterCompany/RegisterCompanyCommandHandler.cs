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
    IOpaqueTokenGenerator tokenGenerator)
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

        var subdomain = await ResolveSubdomainAsync(request.CompanyName, request.Subdomain, cancellationToken);
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

        // Aktivasyon bağlantısı (parola belirleme e-postası) burada GÖNDERİLMEZ: yönetici e-postası
        // henüz doğrulanmadı. VerifyEmailCommandHandler, e-posta doğrulama kodu onaylandıktan sonra
        // taze bir aktivasyon token'ı üretip o e-postayı gönderir - böylece sahibi olmadığı bir
        // adresle firma "aktive edilmiş" gibi görünmez.

        return new RegisterCompanyResponse(
            company.Id, adminUser.Id, company.Name, adminUser.Email.Value,
            company.Subdomain, $"https://{company.Subdomain}.{BaseDomain}", company.CreatedAtUtc);
    }

    private async Task<string> ResolveSubdomainAsync(
        string companyName, string? preferredSubdomain, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(preferredSubdomain))
            return await ResolveUniqueSubdomainAsync(companyName, cancellationToken);

        if (await companyRepository.ExistsBySubdomainAsync(preferredSubdomain, cancellationToken))
            throw new DomainException("subdomain_taken", "Bu site adı zaten kullanılıyor.");

        return preferredSubdomain;
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
