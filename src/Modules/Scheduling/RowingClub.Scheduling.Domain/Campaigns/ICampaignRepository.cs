namespace RowingClub.Scheduling.Domain.Campaigns;

public interface ICampaignRepository
{
    Task<List<Campaign>> GetAllAsync(CancellationToken cancellationToken);

    Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Campaign campaign);

    void Remove(Campaign campaign);
}
