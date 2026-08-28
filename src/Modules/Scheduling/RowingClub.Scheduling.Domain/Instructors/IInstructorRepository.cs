namespace RowingClub.Scheduling.Domain.Instructors;

public interface IInstructorRepository
{
    Task<List<Instructor>> GetAllAsync(CancellationToken cancellationToken);

    Task<Instructor?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Instructor instructor);

    void Remove(Instructor instructor);
}
