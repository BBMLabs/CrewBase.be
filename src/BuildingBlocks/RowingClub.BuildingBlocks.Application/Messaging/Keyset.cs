using System.Text;

namespace RowingClub.BuildingBlocks.Application.Messaging;

public sealed record KeysetResult<T>(IReadOnlyList<T> Items, string? NextCursor, long? TotalCount = null);

public static class KeysetCursor
{
    private static readonly string Separator = ((char)1).ToString();

    public static string Encode(Guid id, params string[] keyParts) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(Separator, keyParts.Append(id.ToString()))));

    public static bool TryDecode(string? cursor, int keyPartCount, out string[] keyParts, out Guid id)
    {
        keyParts = [];
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(cursor)) return false;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split(Separator);
            if (parts.Length != keyPartCount + 1 || !Guid.TryParse(parts[^1], out id)) return false;
            keyParts = parts[..^1];
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public static class KeysetPage
{
    public static List<T> SliceAfterCursor<T>(IEnumerable<T> orderedSource, Func<T, bool> isAfterCursor, int take) =>
        orderedSource.Where(isAfterCursor).Take(take).ToList();

    public static (List<T> Items, string? NextCursor) Trim<T>(
        List<T> fetchedWithExtra, int take, Func<T, Guid> id, Func<T, string[]> keyParts)
    {
        var hasMore = fetchedWithExtra.Count > take;
        var items = hasMore ? fetchedWithExtra.Take(take).ToList() : fetchedWithExtra;
        var nextCursor = hasMore ? KeysetCursor.Encode(id(items[^1]), keyParts(items[^1])) : null;
        return (items, nextCursor);
    }
}
