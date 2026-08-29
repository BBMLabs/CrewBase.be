namespace RowingClub.Identity.Domain.Platform;

public sealed class PlatformActivityLog
{
    public Guid Id { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string ActorEmail { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public Guid? TargetCompanyId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset AtUtc { get; private set; }

    public static PlatformActivityLog Record(
        Guid actorUserId, string actorEmail, string action, Guid? targetCompanyId,
        string? ipAddress, string? userAgent) => new()
    {
        Id = Guid.NewGuid(),
        ActorUserId = actorUserId,
        ActorEmail = actorEmail,
        Action = action,
        TargetCompanyId = targetCompanyId,
        IpAddress = ipAddress,
        UserAgent = userAgent,
        AtUtc = DateTimeOffset.UtcNow,
    };
}
