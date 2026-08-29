namespace RowingClub.Identity.Domain.Platform;

public interface IPlatformActivityLogRepository
{
    Task<(List<PlatformActivityLog> Items, int TotalCount)> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken);
}
