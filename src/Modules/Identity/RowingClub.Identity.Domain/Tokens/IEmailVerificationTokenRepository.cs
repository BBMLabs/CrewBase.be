namespace RowingClub.Identity.Domain.Tokens;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(EmailVerificationToken token);
}
