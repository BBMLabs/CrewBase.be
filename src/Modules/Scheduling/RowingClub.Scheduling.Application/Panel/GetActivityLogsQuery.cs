using MediatR;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record GetActivityLogsQuery(int Take, string? Search = null) : IRequest<List<ActivityLogDto>>;

public sealed record ActivityLogDto(
    Guid ActorUserId, string ActorEmail, string? ActorRole, string Action,
    string? IpAddress, string? UserAgent, DateTimeOffset AtUtc);

public sealed class GetActivityLogsQueryHandler(IActivityLogRepository repository)
    : IRequestHandler<GetActivityLogsQuery, List<ActivityLogDto>>
{
    public async Task<List<ActivityLogDto>> Handle(GetActivityLogsQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 500);
        var logs = await repository.GetRecentAsync(take, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            logs = logs.Where(l =>
                l.ActorEmail.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                l.Action.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (l.IpAddress?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        return logs
            .Select(l => new ActivityLogDto(l.ActorUserId, l.ActorEmail, l.ActorRole, l.Action, l.IpAddress, l.UserAgent, l.AtUtc))
            .ToList();
    }
}
