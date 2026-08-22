namespace RowingClub.Scheduling.Domain.Boats;

public interface IBoatRepository
{
    Task<List<Boat>> GetAllAsync(CancellationToken cancellationToken);

    Task<Boat?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Boat boat);
}
