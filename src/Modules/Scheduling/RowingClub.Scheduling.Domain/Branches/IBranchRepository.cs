namespace RowingClub.Scheduling.Domain.Branches;

public interface IBranchRepository
{
    Task<List<Branch>> GetAllAsync(CancellationToken cancellationToken);

    Task<List<Branch>> GetPageAsync(
        string? search, bool? isActive, string? cursorName, Guid? cursorId, int take, CancellationToken cancellationToken);

    Task<int> CountAsync(string? search, bool? isActive, CancellationToken cancellationToken);

    Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Branch?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);

    Task<int> CountActiveAsync(CancellationToken cancellationToken);

    void Add(Branch branch);

    void Remove(Branch branch);
}
