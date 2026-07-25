using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Application.TwoFactor;

public sealed record EnableTwoFactorCommand(string Method, string? Code, string? Secret) : ICommand<Unit>;

public sealed class EnableTwoFactorCommandHandler(
    ICurrentUser currentUser,
    IUserRepository userRepository,
    IAuditLogger auditLogger)
    : IRequestHandler<EnableTwoFactorCommand, Unit>
{
    public async Task<Unit> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new DomainException("user_not_found", "Kullanıcı bulunamadı.");

        if (request.Method == "Totp")
        {
            if (string.IsNullOrEmpty(request.Secret) || string.IsNullOrEmpty(request.Code))
                throw new DomainException("invalid_2fa_setup", "TOTP kurulumu için secret ve doğrulama kodu gereklidir.");

            if (!TwoFactorService.VerifyTotpCode(request.Secret, request.Code))
                throw new DomainException("invalid_2fa_code", "Doğrulama kodu geçersiz.");
        }

        user.EnableTwoFactor(request.Method, request.Method == "Totp" ? request.Secret : null);
        auditLogger.Log("2FA_ENABLED", user.Id.ToString(), $"2FA yöntemi: {request.Method}");

        return Unit.Value;
    }
}
