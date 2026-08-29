namespace RowingClub.BuildingBlocks.Application.Abstractions;

public interface IPlatformActivityLogWriter
{
    Task RecordAsync(
        string action, Guid? targetCompanyId, string? ipAddress, string? userAgent,
        CancellationToken cancellationToken);
}
