using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.EmailVerification;

public sealed class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository verificationTokenRepository,
    IRefreshTokenHasher tokenHasher)
    : IRequestHandler<VerifyEmailCommand, Unit>
{
    public async Task<Unit> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);
        var user = await userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new DomainException("verification_failed", "Doğrulama başarısız.");

        if (user.EmailVerified)
            return Unit.Value;

        var tokenHash = tokenHasher.Hash(request.Token);
        var storedToken = await verificationTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new DomainException("verification_failed", "Doğrulama bağlantısı geçersiz.");

        if (storedToken.UserId != user.Id)
            throw new DomainException("verification_failed", "Doğrulama bağlantısı geçersiz.");

        storedToken.MarkUsed();
        user.VerifyEmail();

        return Unit.Value;
    }
}
