namespace RowingClub.Scheduling.Domain.Packages;

public interface ILessonPackageRepository
{
    Task<List<LessonPackage>> GetAllAsync(CancellationToken cancellationToken);

    Task<List<LessonPackage>> GetPageAsync(
        string? search, decimal? cursorPrice, Guid? cursorId, int take, CancellationToken cancellationToken);

    Task<int> CountAsync(string? search, CancellationToken cancellationToken);

    Task<LessonPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(LessonPackage lessonPackage);

    void Remove(LessonPackage lessonPackage);
}
