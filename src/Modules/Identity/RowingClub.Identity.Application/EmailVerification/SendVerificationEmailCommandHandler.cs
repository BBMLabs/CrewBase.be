using MediatR;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.EmailVerification;

public sealed class SendVerificationEmailCommandHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository verificationTokenRepository,
    IRefreshTokenHasher tokenHasher,
    IEmailSender emailSender)
    : IRequestHandler<SendVerificationEmailCommand, Unit>
{
    public async Task<Unit> Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || user.EmailVerified)
            return Unit.Value;

        var code = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var verificationToken = EmailVerificationToken.Issue(
            user.Id, tokenHasher.Hash(code), TimeSpan.FromMinutes(1));
        verificationTokenRepository.Add(verificationToken);

        var bodyHtml = $"""
            <p>E-posta adresinizi doğrulamak için kodunuz:</p>
            <p style="font-size:32px;font-weight:700;letter-spacing:8px;font-family:monospace;color:#155e75;margin:20px 0;">{code}</p>
            <p>Bu kod 1 dakika geçerlidir. İşlemi siz başlatmadıysanız bu e-postayı yok sayın.</p>
            """;
        await emailSender.SendAsync(new EmailMessage(
            request.Email,
            "E-posta Doğrulama Kodu",
            EmailTemplate.Render("E-postanızı Doğrulayın", bodyHtml)), cancellationToken);

        return Unit.Value;
    }
}
