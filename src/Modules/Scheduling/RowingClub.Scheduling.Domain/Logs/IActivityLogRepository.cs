namespace RowingClub.Scheduling.Domain.Logs;

public interface IActivityLogRepository
{
    /// <summary>
    /// Keyset-paginated activity log sayfası döner.
    /// </summary>
    Task<List<ActivityLog>> GetPageAsync(
        string? search,
        DateTimeOffset? cursorAtUtc,
        Guid? cursorId,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Opsiyonel arama filtresiyle toplam kayıt sayısını döner.
    /// </summary>
    Task<int> CountAsync(string? search, CancellationToken cancellationToken);

    /// <summary>
    /// Son N kaydı döner (dashboard widget'ı için).
    /// </summary>
    Task<List<ActivityLog>> GetRecentAsync(int take, CancellationToken cancellationToken);

    void Add(ActivityLog log);
}
