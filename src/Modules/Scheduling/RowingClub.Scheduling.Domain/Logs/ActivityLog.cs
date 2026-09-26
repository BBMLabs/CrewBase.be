namespace RowingClub.Scheduling.Domain.Logs;

/// <summary>
/// Platform / panel aktivite logu. Kimin, ne zaman, hangi IP'den hangi işlemi yaptığını tutar.
/// </summary>
public sealed class ActivityLog
{
    public Guid Id { get; private set; }

    public Guid ActorUserId { get; private set; }

    public string ActorEmail { get; private set; } = string.Empty;

    public string? ActorRole { get; private set; }

    /// <summary>Kısa aksiyon adı, ör. "APPOINTMENT_CREATED", "MEMBER_DELETED".</summary>
    public string Action { get; private set; } = string.Empty;

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTimeOffset AtUtc { get; private set; }

    private ActivityLog() { }

    public static ActivityLog Record(
        Guid actorUserId,
        string actorEmail,
        string? actorRole,
        string action,
        string? ipAddress = null,
        string? userAgent = null) => new()
    {
        Id = Guid.NewGuid(),
        ActorUserId = actorUserId,
        ActorEmail = actorEmail.Trim(),
        ActorRole = actorRole,
        Action = action.Trim(),
        IpAddress = ipAddress,
        UserAgent = userAgent,
        AtUtc = DateTimeOffset.UtcNow,
    };
}
