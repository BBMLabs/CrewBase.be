namespace RowingClub.Scheduling.Domain.Boats;

public interface IBoatRepository
{
    Task<List<Boat>> GetAllAsync(CancellationToken cancellationToken);

    Task<Boat?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<Boat>> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken);

    Task<int> CountActiveAsync(CancellationToken cancellationToken);

    void Add(Boat boat);

    void Remove(Boat boat);
}
