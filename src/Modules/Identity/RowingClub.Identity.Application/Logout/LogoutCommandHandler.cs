using MediatR;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.Identity.Application.Logout;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserSessionRepository userSessionRepository,
    IRefreshTokenHasher refreshTokenHasher)
    : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenHasher.Hash(request.RefreshToken);

        var token = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (token is null)
        {
            // Logout is idempotent by design - an unknown/already-invalid token is not an error.
            return Unit.Value;
        }

        token.Revoke();

        var session = await userSessionRepository.GetByRefreshTokenFamilyIdAsync(
            token.FamilyId, cancellationToken);
        session?.Revoke();

        return Unit.Value;
    }
}
