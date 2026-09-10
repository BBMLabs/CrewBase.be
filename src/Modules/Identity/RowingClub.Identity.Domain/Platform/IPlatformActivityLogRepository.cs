namespace RowingClub.Identity.Domain.Platform;

public interface IPlatformActivityLogRepository
{
    Task<List<PlatformActivityLog>> GetPageAsync(
        string? search, DateTimeOffset? cursorAtUtc, Guid? cursorId, int take, CancellationToken cancellationToken);

    Task<int> CountAsync(string? search, CancellationToken cancellationToken);
}
