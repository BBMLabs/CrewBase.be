namespace RowingClub.Identity.Domain.Tokens;

public interface IPendingTwoFactorTokenRepository
{
    Task<PendingTwoFactorToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(PendingTwoFactorToken token);

    void Update(PendingTwoFactorToken token);
}
