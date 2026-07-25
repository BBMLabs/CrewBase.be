using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.PasswordReset;

public sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    ICredentialRepository credentialRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IRefreshTokenHasher tokenHasher,
    IPasswordHasher passwordHasher)
    : IRequestHandler<ResetPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);
        var user = await userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new DomainException("invalid_reset", "Parola sıfırlama bağlantısı geçersiz.");

        var tokenHash = tokenHasher.Hash(request.Token);
        var resetToken = await passwordResetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new DomainException("invalid_reset", "Parola sıfırlama bağlantısı geçersiz.");

        if (resetToken.UserId != user.Id || !resetToken.IsValid)
            throw new DomainException("invalid_reset", "Parola sıfırlama bağlantısı geçersiz veya süresi dolmuş.");

        resetToken.MarkUsed();

        var credential = await credentialRepository.GetByUserIdAsync(user.Id, cancellationToken)
            ?? throw new DomainException("invalid_reset", "Kullanıcı bilgileri bulunamadı.");

        credential.ChangePasswordHash(passwordHasher.Hash(request.NewPassword));
        return Unit.Value;
    }
}
