using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Platform;

namespace RowingClub.Identity.Infrastructure;

public sealed class PlatformActivityLogWriter(RowingClubDbContext context, ICurrentUser currentUser) : IPlatformActivityLogWriter
{
    public async Task RecordAsync(
        string action, Guid? targetCompanyId, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        context.Set<PlatformActivityLog>().Add(
            PlatformActivityLog.Record(currentUser.UserId, currentUser.Email, action, targetCompanyId, ipAddress, userAgent));
        await context.SaveChangesAsync(cancellationToken);
    }
}
