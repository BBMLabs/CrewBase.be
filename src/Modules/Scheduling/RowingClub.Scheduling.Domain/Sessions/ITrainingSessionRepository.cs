using RowingClub.Scheduling.Domain.Boats;

namespace RowingClub.Scheduling.Domain.Sessions;

public interface ITrainingSessionRepository
{
    /// <summary>Aynı slot + sınıf + derecede boş koltuğu olan seansı bulur (üyeleri gruplamak için).</summary>
    Task<TrainingSession?> FindJoinableAsync(
        DateOnly date, TimeOnly startTime, BoatClass boatClass, int level, CancellationToken cancellationToken);

    /// <summary>Verilen slottaki tüm seanslar (tekne/eğitmen çakışması kontrolü için).</summary>
    Task<List<TrainingSession>> GetBySlotAsync(
        DateOnly date, TimeOnly startTime, CancellationToken cancellationToken);

    Task<List<TrainingSession>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken);

    Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(TrainingSession session);
}
