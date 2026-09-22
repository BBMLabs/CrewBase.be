namespace RowingClub.Identity.Domain.Tokens;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>All tokens ever issued in the family, active or not - needed to revoke the whole
    /// chain when reuse of a revoked token is detected (spec section 11).</summary>
    Task<IReadOnlyCollection<RefreshToken>> GetFamilyAsync(Guid familyId, CancellationToken cancellationToken);

    Task<List<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    void Add(RefreshToken refreshToken);

    void RemoveRange(IEnumerable<RefreshToken> refreshTokens);
}
