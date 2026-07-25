using MediatR;
using RowingClub.BuildingBlocks.Security.Tokens;
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
    IEmailSender emailSender)
    : IRequestHandler<ForgotPasswordCommand, Unit>
{
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

        var resetLink = $"https://rowingclub.dev/reset-password?token={rawToken}&email={request.Email}";
        await emailSender.SendAsync(new EmailMessage(
            request.Email,
            "Parola Sıfırlama",
            $"<p>Parolanızı sıfırlamak için <a href='{resetLink}'>bu bağlantıya tıklayın</a>. Bu bağlantı 1 saat geçerlidir.</p>"), cancellationToken);

        return Unit.Value;
    }
}
