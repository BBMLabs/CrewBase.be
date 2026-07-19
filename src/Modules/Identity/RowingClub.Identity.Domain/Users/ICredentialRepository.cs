namespace RowingClub.Identity.Domain.Users;

public interface ICredentialRepository
{
    Task<Credential?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    void Add(Credential credential);
}
