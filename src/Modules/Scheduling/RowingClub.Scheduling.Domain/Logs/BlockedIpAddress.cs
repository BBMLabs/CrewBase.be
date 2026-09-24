namespace RowingClub.Scheduling.Domain.Logs;

/// <summary>
/// IP engel kaydı. Belirli bir IP'yi panelden manuel olarak engellemek için kullanılır.
/// </summary>
public sealed class BlockedIpAddress
{
    public Guid Id { get; private set; }

    public string IpAddress { get; private set; } = string.Empty;

    public string? Reason { get; private set; }

    public Guid BlockedByUserId { get; private set; }

    public DateTimeOffset BlockedAtUtc { get; private set; }

    private BlockedIpAddress() { }

    public static BlockedIpAddress Create(string ipAddress, string? reason, Guid blockedByUserId) => new()
    {
        Id = Guid.NewGuid(),
        IpAddress = ipAddress.Trim(),
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
        BlockedByUserId = blockedByUserId,
        BlockedAtUtc = DateTimeOffset.UtcNow,
    };
}
