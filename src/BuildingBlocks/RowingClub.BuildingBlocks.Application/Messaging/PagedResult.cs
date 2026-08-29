namespace RowingClub.BuildingBlocks.Application.Messaging;

public sealed record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public static PagedResult<T> Create(List<T> all, int page, int pageSize)
    {
        var clampedPage = Math.Max(1, page);
        var clampedPageSize = Math.Clamp(pageSize, 1, 200);
        var items = all.Skip((clampedPage - 1) * clampedPageSize).Take(clampedPageSize).ToList();
        return new PagedResult<T>(items, all.Count, clampedPage, clampedPageSize);
    }
}
