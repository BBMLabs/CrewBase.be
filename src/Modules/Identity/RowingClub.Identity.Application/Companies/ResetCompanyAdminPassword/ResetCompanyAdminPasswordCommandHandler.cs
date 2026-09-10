using MediatR;
using Microsoft.Extensions.Configuration;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Application.Companies.ResetCompanyAdminPassword;

public sealed class ResetCompanyAdminPasswordCommandHandler(
    ICompanyRepository companyRepository,
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IOpaqueTokenGenerator tokenGenerator,
    IRefreshTokenHasher tokenHasher,
    IEmailSender emailSender,
    IConfiguration configuration,
    IAuditLogger auditLogger)
    : IRequestHandler<ResetCompanyAdminPasswordCommand, Unit>
{
    private const string DefaultPublicAppUrl = "https://faturebase.com";

    public async Task<Unit> Handle(ResetCompanyAdminPasswordCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        var companyUsers = await userRepository.GetByCompanyIdAsync(company.Id, cancellationToken);
        var admin = companyUsers.FirstOrDefault(u => u.Role == UserRole.CompanyAdmin)
            ?? throw new DomainException("company_admin_not_found", "Şirketin bir yöneticisi bulunamadı.");

        var rawToken = tokenGenerator.Generate();
        var tokenHash = tokenHasher.Hash(rawToken);
        var resetToken = PasswordResetToken.Issue(admin.Id, tokenHash, TimeSpan.FromHours(1));
        passwordResetTokenRepository.Add(resetToken);

        var publicAppUrl = (configuration["PUBLIC_APP_URL"] ?? DefaultPublicAppUrl).TrimEnd('/');
        var resetLink = $"{publicAppUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(admin.Email.Value)}";

        try
        {
            var bodyHtml = "<p>Platform yöneticisi hesabınız için bir parola sıfırlama talebinde bulundu. Aşağıdaki bağlantı 1 saat geçerlidir.</p>";
            var htmlBody = EmailTemplate.Render("Parola Sıfırlama", bodyHtml, "Parolamı Sıfırla", resetLink);
            await emailSender.SendAsync(new EmailMessage(admin.Email.Value, "Parola Sıfırlama", htmlBody), cancellationToken);
        }
        catch (Exception ex)
        {
            auditLogger.Log("PASSWORD_RESET_EMAIL_FAILED", admin.Email.Value, $"Parola sıfırlama e-postası gönderilemedi: {ex.Message}");
        }

        auditLogger.Log("COMPANY_ADMIN_PASSWORD_RESET_TRIGGERED", company.Id.ToString(), $"Platform admin tarafından {admin.Email.Value} için parola sıfırlama tetiklendi.");

        return Unit.Value;
    }
}
