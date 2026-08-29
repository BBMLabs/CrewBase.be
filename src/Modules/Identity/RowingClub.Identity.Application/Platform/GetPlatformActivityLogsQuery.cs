using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Platform;

namespace RowingClub.Identity.Application.Platform;

public sealed record GetPlatformActivityLogsQuery(string? Search = null, int Page = 1, int PageSize = 25)
    : IRequest<PagedResult<PlatformActivityLogDto>>;

public sealed record PlatformActivityLogDto(
    Guid ActorUserId, string ActorEmail, string Action, Guid? TargetCompanyId, string? TargetCompanyName,
    string? IpAddress, string? UserAgent, DateTimeOffset AtUtc);

public sealed class GetPlatformActivityLogsQueryHandler(
    IPlatformActivityLogRepository repository, ICompanyRepository companyRepository)
    : IRequestHandler<GetPlatformActivityLogsQuery, PagedResult<PlatformActivityLogDto>>
{
    public async Task<PagedResult<PlatformActivityLogDto>> Handle(
        GetPlatformActivityLogsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var (logs, totalCount) = await repository.GetPagedAsync(request.Search, page, pageSize, cancellationToken);

        var companyIds = logs.Where(l => l.TargetCompanyId.HasValue).Select(l => l.TargetCompanyId!.Value).Distinct().ToList();
        var companies = await companyRepository.GetByIdsAsync(companyIds, cancellationToken);
        var companyNames = companies.ToDictionary(c => c.Id, c => c.Name);

        var dtos = logs
            .Select(l => new PlatformActivityLogDto(
                l.ActorUserId, l.ActorEmail, l.Action, l.TargetCompanyId,
                l.TargetCompanyId.HasValue ? companyNames.GetValueOrDefault(l.TargetCompanyId.Value) : null,
                l.IpAddress, l.UserAgent, l.AtUtc))
            .ToList();

        return new PagedResult<PlatformActivityLogDto>(dtos, totalCount, page, pageSize);
    }
}
