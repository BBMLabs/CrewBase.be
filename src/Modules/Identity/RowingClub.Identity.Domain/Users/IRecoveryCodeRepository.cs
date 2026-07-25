namespace RowingClub.Identity.Domain.Users;

public interface IRecoveryCodeRepository
{
    Task<List<UserRecoveryCode>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<int> GetUnusedCountAsync(Guid userId, CancellationToken cancellationToken);

    void AddRange(IEnumerable<UserRecoveryCode> codes);

    void Update(UserRecoveryCode code);
}
