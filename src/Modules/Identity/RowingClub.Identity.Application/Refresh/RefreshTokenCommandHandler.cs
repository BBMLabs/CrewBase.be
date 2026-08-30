using MediatR;
using Microsoft.Extensions.Configuration;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Application.Tokens;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Application.Refresh;

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserSessionRepository userSessionRepository,
    IUserRepository userRepository,
    IRefreshTokenHasher refreshTokenHasher,
    TokenPairIssuer tokenPairIssuer,
    IAuditLogger auditLogger,
    IEmailSender emailSender,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IOpaqueTokenGenerator opaqueTokenGenerator,
    IConfiguration configuration)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private const string DefaultPublicAppUrl = "https://faturebase.com";

    private static readonly AuthenticationFailedException InvalidRefreshToken =
        new("Refresh token geçersiz veya süresi dolmuş.");

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenHasher.Hash(request.RefreshToken);

        var presentedToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw InvalidRefreshToken;

        if (presentedToken.RevokedAtUtc is not null)
        {
            await RevokeAllForUser(presentedToken.UserId, presentedToken.FamilyId, cancellationToken);

            auditLogger.Log("TOKEN_THEFT", presentedToken.UserId.ToString(),
                $"MUHTEMEL TOKEN HIRSIZLIĞI: Daha önce iptal edilmiş refresh token ({presentedToken.Id}) tekrar kullanılmaya çalışıldı. Tüm oturumlar iptal edildi.");

            var theftUser = await userRepository.GetByIdAsync(presentedToken.UserId, cancellationToken);
            if (theftUser is not null)
            {
                var rawResetToken = opaqueTokenGenerator.Generate();
                var resetTokenHash = refreshTokenHasher.Hash(rawResetToken);
                var resetToken = PasswordResetToken.Issue(theftUser.Id, resetTokenHash, TimeSpan.FromHours(1));
                passwordResetTokenRepository.Add(resetToken);

                var publicAppUrl = (configuration["PUBLIC_APP_URL"] ?? DefaultPublicAppUrl).TrimEnd('/');
                var resetLink = $"{publicAppUrl}/parola-sifirla?token={Uri.EscapeDataString(rawResetToken)}&email={Uri.EscapeDataString(theftUser.Email.Value)}";

                var bodyHtml = "<p>Hesabınızda şüpheli aktivite tespit edildi. Tüm oturumlarınız güvenlik amacıyla kapatılmıştır.</p><p>Eğer bu siz değilseniz, lütfen hemen şifrenizi değiştirin.</p>";
                var htmlBody = EmailTemplate.Render("Güvenlik Uyarısı", bodyHtml, "Şifremi Değiştir", resetLink);
                var msg = new EmailMessage(theftUser.Email.Value, "Hesabınızda Şüpheli Aktivite Tespit Edildi", htmlBody);
                await emailSender.SendAsync(msg, cancellationToken);
            }

            throw InvalidRefreshToken;
        }

        if (!presentedToken.IsActive)
        {
            throw InvalidRefreshToken;
        }

        var existingSession = await userSessionRepository.GetByRefreshTokenFamilyIdAsync(
            presentedToken.FamilyId, cancellationToken);

        if (existingSession is null || !existingSession.IsActive)
        {
            throw InvalidRefreshToken;
        }

        var user = await userRepository.GetByIdAsync(presentedToken.UserId, cancellationToken)
            ?? throw InvalidRefreshToken;

        user.EnsureCanAuthenticate();

        var role = user.Role.ToString();
        var (tokenPair, newRefreshToken) = tokenPairIssuer.RotateWithinFamily(
            user.Id, user.Email.Value, presentedToken.FamilyId, role, user.CompanyId);

        presentedToken.MarkReplacedBy(newRefreshToken.Id);

        existingSession.Touch();

        return new RefreshTokenResponse(
            tokenPair.AccessToken, tokenPair.AccessTokenExpiresAtUtc, tokenPair.RefreshToken);
    }

    private async Task RevokeAllForUser(Guid userId, Guid familyId, CancellationToken cancellationToken)
    {
        var family = await refreshTokenRepository.GetFamilyAsync(familyId, cancellationToken);
        foreach (var sibling in family.Where(t => t.IsActive))
        {
            sibling.Revoke();
        }

        var sessions = await userSessionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        foreach (var session in sessions)
        {
            session.Revoke();
        }
    }
}
