namespace RowingClub.Identity.Domain.Tokens;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByRefreshTokenFamilyIdAsync(Guid familyId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    void Add(UserSession session);
}
