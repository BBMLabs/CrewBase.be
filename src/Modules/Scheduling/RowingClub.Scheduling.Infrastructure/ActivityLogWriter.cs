using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Infrastructure.Persistence;

namespace RowingClub.Scheduling.Infrastructure;

/// <summary>
/// Kendi bağımsız kaydını yapar (ana komutun unit-of-work'ünden ayrı) - hangi modülün komutu
/// olduğuna bakılmaksızın (Identity veya Scheduling) tek bir yerden tenant DB'ye yazar.
/// </summary>
public sealed class ActivityLogWriter(TenantDbContext context, ICurrentUser currentUser) : IActivityLogWriter
{
    public async Task RecordAsync(string action, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        context.ActivityLogs.Add(
            ActivityLog.Record(currentUser.UserId, currentUser.Email, currentUser.Role, action, ipAddress, userAgent));
        await context.SaveChangesAsync(cancellationToken);
    }
}
