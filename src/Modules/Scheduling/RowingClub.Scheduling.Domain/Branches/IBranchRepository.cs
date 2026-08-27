namespace RowingClub.Scheduling.Domain.Branches;

public interface IBranchRepository
{
    Task<List<Branch>> GetAllAsync(CancellationToken cancellationToken);

    Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Branch?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);

    Task<int> CountActiveAsync(CancellationToken cancellationToken);

    void Add(Branch branch);
}
