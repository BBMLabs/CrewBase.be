namespace RowingClub.Scheduling.Domain.Customers;

public interface IMemberPasswordSetupTokenRepository
{
    Task<MemberPasswordSetupToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(MemberPasswordSetupToken token);
}
