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
    IOpaqueTokenGenerator tokenGenerator,
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

        var rawToken = tokenGenerator.Generate();
        var tokenHash = tokenHasher.Hash(rawToken);
        var verificationToken = EmailVerificationToken.Issue(user.Id, tokenHash, TimeSpan.FromHours(24));
        verificationTokenRepository.Add(verificationToken);

        var verifyLink = $"https://rowingclub.dev/verify-email?token={rawToken}&email={request.Email}";
        await emailSender.SendAsync(new EmailMessage(
            request.Email,
            "E-posta Doğrulama",
            $"<p>E-posta adresinizi doğrulamak için <a href='{verifyLink}'>bu bağlantıya tıklayın</a>. Bu bağlantı 24 saat geçerlidir.</p>"), cancellationToken);

        return Unit.Value;
    }
}
