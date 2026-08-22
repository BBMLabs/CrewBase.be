namespace RowingClub.Scheduling.Domain.Settings;

public interface ISettingsRepository
{
    /// <summary>Tenant veritabanındaki tek ayar satırını döner (provision sırasında tohumlanır).</summary>
    Task<CompanySettings?> GetAsync(CancellationToken cancellationToken);

    void Add(CompanySettings settings);
}
