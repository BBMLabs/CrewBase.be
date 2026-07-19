using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    void Add(User user);
}
