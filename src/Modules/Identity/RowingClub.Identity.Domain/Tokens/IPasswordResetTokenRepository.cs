namespace RowingClub.Identity.Domain.Tokens;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(PasswordResetToken token);
}
