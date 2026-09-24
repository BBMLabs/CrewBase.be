using RowingClub.Scheduling.Domain.Boats;

namespace RowingClub.Scheduling.Domain.Sessions;

public interface ITrainingSessionRepository
{
    Task<TrainingSession?> FindJoinableLowestLevelAsync(
        DateOnly date, TimeOnly startTime, BoatClass boatClass, CancellationToken cancellationToken);

    Task<TrainingSession?> FindMutualTeammateSessionAsync(
        DateOnly date, TimeOnly startTime, BoatClass boatClass,
        string callerFullName, string teammateFullName, CancellationToken cancellationToken);

    /// <summary>Verilen slottaki tüm seanslar (tekne/eğitmen çakışması kontrolü için).</summary>
    Task<List<TrainingSession>> GetBySlotAsync(
        DateOnly date, TimeOnly startTime, CancellationToken cancellationToken);

    Task<List<TrainingSession>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken);

    Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(TrainingSession session);
}
