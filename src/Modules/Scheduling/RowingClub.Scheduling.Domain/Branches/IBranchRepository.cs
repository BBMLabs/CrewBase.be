namespace RowingClub.Scheduling.Domain.Branches;

public interface IBranchRepository
{
    Task<List<Branch>> GetAllAsync(CancellationToken cancellationToken);

    Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Branch branch);
}
