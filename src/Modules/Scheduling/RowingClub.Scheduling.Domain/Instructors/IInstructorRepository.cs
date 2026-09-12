namespace RowingClub.Scheduling.Domain.Instructors;

public interface IInstructorRepository
{
    Task<List<Instructor>> GetAllAsync(CancellationToken cancellationToken);

    Task<Instructor?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<Instructor>> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken);

    Task<int> CountActiveAsync(CancellationToken cancellationToken);

    void Add(Instructor instructor);

    void Remove(Instructor instructor);
}
