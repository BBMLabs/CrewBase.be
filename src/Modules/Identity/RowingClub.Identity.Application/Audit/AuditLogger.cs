using Microsoft.Extensions.Logging;

namespace RowingClub.Identity.Application.Audit;

public interface IAuditLogger
{
    void Log(string eventType, string userId, string details);
}

public sealed class AuditLogger(ILogger<AuditLogger> logger) : IAuditLogger
{
    public void Log(string eventType, string userId, string details)
    {
        logger.LogInformation("AUDIT [{EventType}] User: {UserId} | {Details}", eventType, userId, details);
    }
}
