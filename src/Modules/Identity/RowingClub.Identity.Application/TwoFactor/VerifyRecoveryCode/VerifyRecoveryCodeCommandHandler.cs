using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Application.TwoFactor.VerifyRecoveryCode;

public sealed class VerifyRecoveryCodeCommandHandler(
    IRecoveryCodeRepository recoveryCodeRepository,
    IRefreshTokenHasher refreshTokenHasher)
    : IRequestHandler<VerifyRecoveryCodeCommand, Unit>
{
    public async Task<Unit> Handle(VerifyRecoveryCodeCommand request, CancellationToken cancellationToken)
    {
        var codeHash = refreshTokenHasher.Hash(request.RecoveryCode);

        var codes = await recoveryCodeRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var matched = codes.FirstOrDefault(c => c.CodeHash == codeHash && !c.IsUsed);

        if (matched is null)
        {
            throw new DomainException("invalid_recovery_code", "Kurtarma kodu geçersiz veya daha önce kullanılmış.");
        }

        matched.MarkUsed();
        recoveryCodeRepository.Update(matched);

        return Unit.Value;
    }
}
