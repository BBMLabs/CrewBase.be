using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application.Tokens;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Application.Refresh;

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserSessionRepository userSessionRepository,
    IUserRepository userRepository,
    IRefreshTokenHasher refreshTokenHasher,
    TokenPairIssuer tokenPairIssuer)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private static readonly AuthenticationFailedException InvalidRefreshToken =
        new("Refresh token geçersiz veya süresi dolmuş.");

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenHasher.Hash(request.RefreshToken);

        var presentedToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw InvalidRefreshToken;

        if (presentedToken.RevokedAtUtc is not null)
        {
            // Already-rotated token presented again -> theft signal. Revoke the whole family.
            presentedToken.FlagReuse();

            var family = await refreshTokenRepository.GetFamilyAsync(presentedToken.FamilyId, cancellationToken);
            foreach (var sibling in family.Where(t => t.Id != presentedToken.Id && t.IsActive))
            {
                sibling.Revoke();
            }

            var session = await userSessionRepository.GetByRefreshTokenFamilyIdAsync(
                presentedToken.FamilyId, cancellationToken);
            session?.Revoke();

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
            // Session was revoked (logout / logout-all) - the family is dead even if this
            // particular token row hasn't expired yet.
            throw InvalidRefreshToken;
        }

        var user = await userRepository.GetByIdAsync(presentedToken.UserId, cancellationToken)
            ?? throw InvalidRefreshToken;

        user.EnsureCanAuthenticate();

        var (tokenPair, newRefreshToken) = tokenPairIssuer.RotateWithinFamily(
            user.Id, user.Email.Value, presentedToken.FamilyId);

        presentedToken.MarkReplacedBy(newRefreshToken.Id);

        existingSession.Touch();

        return new RefreshTokenResponse(
            tokenPair.AccessToken, tokenPair.AccessTokenExpiresAtUtc, tokenPair.RefreshToken);
    }
}
