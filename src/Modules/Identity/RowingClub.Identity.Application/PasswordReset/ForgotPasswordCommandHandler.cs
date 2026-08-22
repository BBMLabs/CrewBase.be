using MediatR;
using Microsoft.Extensions.Configuration;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.PasswordReset;

public sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IOpaqueTokenGenerator tokenGenerator,
    IRefreshTokenHasher tokenHasher,
    IEmailSender emailSender,
    IConfiguration configuration,
    IAuditLogger auditLogger)
    : IRequestHandler<ForgotPasswordCommand, Unit>
{
    private const string DefaultPublicAppUrl = "https://faturebase.com";

    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
            return Unit.Value;

        var rawToken = tokenGenerator.Generate();
        var tokenHash = tokenHasher.Hash(rawToken);
        var resetToken = PasswordResetToken.Issue(user.Id, tokenHash, TimeSpan.FromHours(1));
        passwordResetTokenRepository.Add(resetToken);

        var publicAppUrl = (configuration["PUBLIC_APP_URL"] ?? DefaultPublicAppUrl).TrimEnd('/');
        var resetLink = $"{publicAppUrl}/parola-sifirla?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(request.Email)}";

        // E-posta gönderimi başarısız olsa bile (SMTP kapalı/erişilemez) istek başarılı dönmelidir:
        // aksi halde var/yok olan e-postalar hata davranışıyla birbirinden ayırt edilebilir hale gelir.
        try
        {
            await emailSender.SendAsync(new EmailMessage(
                request.Email,
                "Parola Sıfırlama",
                $"<p>Parolanızı sıfırlamak için <a href='{resetLink}'>bu bağlantıya tıklayın</a>. Bu bağlantı 1 saat geçerlidir.</p>"), cancellationToken);
        }
        catch (Exception ex)
        {
            auditLogger.Log("PASSWORD_RESET_EMAIL_FAILED", request.Email, $"Parola sıfırlama e-postası gönderilemedi: {ex.Message}");
        }

        return Unit.Value;
    }
}
