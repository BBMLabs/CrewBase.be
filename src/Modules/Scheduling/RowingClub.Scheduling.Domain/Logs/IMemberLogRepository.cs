namespace RowingClub.Scheduling.Domain.Logs;

public interface IMemberLogRepository
{
    Task<List<MemberLog>> GetByCustomerAsync(Guid customerId, int take, CancellationToken cancellationToken);

    Task<List<MemberLog>> GetRecentAsync(int take, CancellationToken cancellationToken);

    void Add(MemberLog log);
}
