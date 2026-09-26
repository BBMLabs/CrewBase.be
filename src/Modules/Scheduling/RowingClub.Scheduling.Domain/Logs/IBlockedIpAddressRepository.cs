namespace RowingClub.Scheduling.Domain.Logs;

public interface IBlockedIpAddressRepository
{
    Task<List<BlockedIpAddress>> GetAllAsync(CancellationToken cancellationToken);

    Task<BlockedIpAddress?> GetByIpAsync(string ipAddress, CancellationToken cancellationToken);

    void Add(BlockedIpAddress entry);

    void Remove(BlockedIpAddress entry);
}
