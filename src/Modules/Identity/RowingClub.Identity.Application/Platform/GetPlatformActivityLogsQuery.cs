using System.Globalization;
using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Platform;

namespace RowingClub.Identity.Application.Platform;

public sealed record GetPlatformActivityLogsQuery(string? Search = null, string? Cursor = null, int Limit = 25)
    : IRequest<KeysetResult<PlatformActivityLogDto>>;

public sealed record PlatformActivityLogDto(
    Guid ActorUserId, string ActorEmail, string Action, Guid? TargetCompanyId, string? TargetCompanyName,
    string? IpAddress, string? UserAgent, DateTimeOffset AtUtc);

public sealed class GetPlatformActivityLogsQueryHandler(
    IPlatformActivityLogRepository repository, ICompanyRepository companyRepository)
    : IRequestHandler<GetPlatformActivityLogsQuery, KeysetResult<PlatformActivityLogDto>>
{
    public async Task<KeysetResult<PlatformActivityLogDto>> Handle(
        GetPlatformActivityLogsQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 200);
        var hasCursor = KeysetCursor.TryDecode(request.Cursor, 1, out var keyParts, out var cursorId);
        var cursorAtUtc = hasCursor ? DateTimeOffset.Parse(keyParts[0], CultureInfo.InvariantCulture) : (DateTimeOffset?)null;

        var logs = await repository.GetPageAsync(
            request.Search, cursorAtUtc, hasCursor ? cursorId : null, limit + 1, cancellationToken);

        var companyIds = logs.Where(l => l.TargetCompanyId.HasValue).Select(l => l.TargetCompanyId!.Value).Distinct().ToList();
        var companies = await companyRepository.GetByIdsAsync(companyIds, cancellationToken);
        var companyNames = companies.ToDictionary(c => c.Id, c => c.Name);

        var (page, nextCursor) = KeysetPage.Trim(
            logs, limit, l => l.Id, l => [l.AtUtc.ToString("o", CultureInfo.InvariantCulture)]);

        var dtos = page
            .Select(l => new PlatformActivityLogDto(
                l.ActorUserId, l.ActorEmail, l.Action, l.TargetCompanyId,
                l.TargetCompanyId.HasValue ? companyNames.GetValueOrDefault(l.TargetCompanyId.Value) : null,
                l.IpAddress, l.UserAgent, l.AtUtc))
            .ToList();

        var totalCount = await repository.CountAsync(request.Search, cancellationToken);

        return new KeysetResult<PlatformActivityLogDto>(dtos, nextCursor, totalCount);
    }
}
