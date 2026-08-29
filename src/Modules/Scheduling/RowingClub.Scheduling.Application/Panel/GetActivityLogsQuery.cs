using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record GetActivityLogsQuery(string? Search = null, int Page = 1, int PageSize = 25)
    : IRequest<PagedResult<ActivityLogDto>>;

public sealed record ActivityLogDto(
    Guid ActorUserId, string ActorEmail, string? ActorRole, string Action,
    string? IpAddress, string? UserAgent, DateTimeOffset AtUtc);

public sealed class GetActivityLogsQueryHandler(IActivityLogRepository repository)
    : IRequestHandler<GetActivityLogsQuery, PagedResult<ActivityLogDto>>
{
    public async Task<PagedResult<ActivityLogDto>> Handle(GetActivityLogsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var (logs, totalCount) = await repository.GetPagedAsync(request.Search, page, pageSize, cancellationToken);

        var dtos = logs
            .Select(l => new ActivityLogDto(l.ActorUserId, l.ActorEmail, l.ActorRole, l.Action, l.IpAddress, l.UserAgent, l.AtUtc))
            .ToList();

        return new PagedResult<ActivityLogDto>(dtos, totalCount, page, pageSize);
    }
}
