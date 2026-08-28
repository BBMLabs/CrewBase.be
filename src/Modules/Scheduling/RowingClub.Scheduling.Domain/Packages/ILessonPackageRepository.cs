namespace RowingClub.Scheduling.Domain.Packages;

public interface ILessonPackageRepository
{
    Task<List<LessonPackage>> GetAllAsync(CancellationToken cancellationToken);

    Task<LessonPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(LessonPackage lessonPackage);

    void Remove(LessonPackage lessonPackage);
}
