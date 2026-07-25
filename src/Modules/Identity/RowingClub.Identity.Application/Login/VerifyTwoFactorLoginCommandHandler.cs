using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Application.Tokens;
using RowingClub.Identity.Application.TwoFactor;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Application.Login;

public sealed class VerifyTwoFactorLoginCommandHandler(
    IPendingTwoFactorTokenRepository pendingTwoFactorTokenRepository,
    IRefreshTokenHasher refreshTokenHasher,
    IUserRepository userRepository,
    IUserSessionRepository userSessionRepository,
    TokenPairIssuer tokenPairIssuer,
    IAuditLogger auditLogger,
    IRecoveryCodeRepository recoveryCodeRepository,
    IEmailSender emailSender)
    : IRequestHandler<VerifyTwoFactorLoginCommand, VerifyTwoFactorLoginResponse>
{
    private static readonly AuthenticationFailedException InvalidOrExpired =
        new("2FA kodu veya oturum geçersiz.");

    public async Task<VerifyTwoFactorLoginResponse> Handle(
        VerifyTwoFactorLoginCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenHasher.Hash(request.PendingToken);
        var pendingToken = await pendingTwoFactorTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw InvalidOrExpired;

        if (!pendingToken.IsValid)
            throw InvalidOrExpired;

        var user = await userRepository.GetByIdAsync(pendingToken.UserId, cancellationToken)
            ?? throw InvalidOrExpired;

        user.EnsureCanAuthenticate();

        int? remaining = null;

        var isValidCode = user.TwoFactorMethod switch
        {
            "Totp" => TwoFactorService.VerifyTotpCode(user.TwoFactorSecret!, request.Code),
            "Email" => TwoFactorService.VerifyTotpCode(user.TwoFactorSecret!, request.Code),
            _ => false
        };

        if (!isValidCode)
        {
            remaining = await VerifyRecoveryCode(user, request.Code, cancellationToken);
            if (remaining is null)
            {
                auditLogger.Log("2FA_FAILED", user.Id.ToString(), "Geçersiz 2FA kodu veya kurtarma kodu.");
                throw InvalidOrExpired;
            }

            if (remaining <= 2)
            {
                var msg = new EmailMessage(user.Email.Value,
                    "Kurtarma Kodlarınız Tükeniyor",
                    $"<p>Merhaba,</p><p>{remaining} adet kurtarma kodunuz kaldı. Yeni kod üretmek için hesap ayarlarınızı ziyaret edin.</p><p>RowingClub</p>");
                await emailSender.SendAsync(msg, cancellationToken);
            }
        }

        pendingToken.MarkUsed();
        pendingTwoFactorTokenRepository.Update(pendingToken);

        var role = user.Role.ToString();
        var (tokenPair, refreshToken) = tokenPairIssuer.IssueNewFamily(user.Id, user.Email.Value, role, user.CompanyId);
        userSessionRepository.Add(UserSession.Start(user.Id, refreshToken.FamilyId, pendingToken.DeviceInfo));

        auditLogger.Log("2FA_LOGIN_COMPLETE", user.Id.ToString(), "2FA doğrulaması başarılı, tokenlar verildi.");

        return new VerifyTwoFactorLoginResponse(
            tokenPair.AccessToken, tokenPair.AccessTokenExpiresAtUtc, tokenPair.RefreshToken, remaining);
    }

    private async Task<int?> VerifyRecoveryCode(User user, string code, CancellationToken cancellationToken)
    {
        var codeHash = refreshTokenHasher.Hash(code);
        var recoveryCodes = await recoveryCodeRepository.GetByUserIdAsync(user.Id, cancellationToken);
        var matched = recoveryCodes.FirstOrDefault(c => c.CodeHash == codeHash && !c.IsUsed);

        if (matched is null) return null;

        matched.MarkUsed();
        recoveryCodeRepository.Update(matched);

        auditLogger.Log("2FA_RECOVERY_USED", user.Id.ToString(), "Kurtarma kodu ile 2FA geçildi.");

        var remaining = await recoveryCodeRepository.GetUnusedCountAsync(user.Id, cancellationToken);
        auditLogger.Log("RECOVERY_CODES_REMAINING", user.Id.ToString(), $"Kalan kurtarma kodu sayısı: {remaining}");

        return remaining;
    }
}
