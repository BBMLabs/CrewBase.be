namespace RowingClub.Scheduling.Domain.Settings;

public interface IClosedDateRepository
{
    Task<List<ClosedDate>> GetFromAsync(DateOnly from, CancellationToken cancellationToken);

    Task<ClosedDate?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken);

    Task<bool> IsClosedAsync(DateOnly date, CancellationToken cancellationToken);

    void Add(ClosedDate closedDate);

    void Remove(ClosedDate closedDate);
}
