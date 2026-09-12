using System.Globalization;
using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record GetActivityLogsQuery(string? Search = null, string? Cursor = null, int Limit = 25)
    : IRequest<KeysetResult<ActivityLogDto>>;

public sealed record ActivityLogDto(
    Guid ActorUserId, string ActorEmail, string? ActorRole, string Action,
    string? IpAddress, string? UserAgent, DateTimeOffset AtUtc);

public sealed class GetActivityLogsQueryHandler(IActivityLogRepository repository)
    : IRequestHandler<GetActivityLogsQuery, KeysetResult<ActivityLogDto>>
{
    public async Task<KeysetResult<ActivityLogDto>> Handle(GetActivityLogsQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 200);
        var hasCursor = KeysetCursor.TryDecode(request.Cursor, 1, out var keyParts, out var cursorId);
        var cursorAtUtc = hasCursor ? DateTimeOffset.Parse(keyParts[0], CultureInfo.InvariantCulture) : (DateTimeOffset?)null;

        var logs = await repository.GetPageAsync(
            request.Search, cursorAtUtc, hasCursor ? cursorId : null, limit + 1, cancellationToken);

        var (page, nextCursor) = KeysetPage.Trim(
            logs, limit, l => l.Id, l => [l.AtUtc.ToString("o", CultureInfo.InvariantCulture)]);

        var totalCount = await repository.CountAsync(request.Search, cancellationToken);

        return new KeysetResult<ActivityLogDto>(
            page.Select(l => new ActivityLogDto(l.ActorUserId, l.ActorEmail, l.ActorRole, l.Action, l.IpAddress, l.UserAgent, l.AtUtc)).ToList(),
            nextCursor, totalCount);
    }
}
