namespace RowingClub.Scheduling.Domain.Logs;

/// <summary>
/// Üye hareket kaydı (giriş, randevu, paket düşümü/iadesi, seviye değişimi...). Tenant DB'nin
/// member_logs tablosuna yazılır; Details alanı Infrastructure'da alan-şifreleme ile saklanır.
/// Şema: 20260821200537_AddMembersPackagesLogsClosedDates (Id/CustomerId/Event/Details/AtUtc,
/// index (CustomerId, AtUtc), Customer FK cascade).
/// </summary>
public sealed class MemberLog
{
    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    /// <summary>Olay adı; MemberEvents sabitleri veya kısa özel adlar (CONSENTS_SUBMITTED gibi). Maks 60.</summary>
    public string Event { get; private set; } = string.Empty;

    public string? Details { get; private set; }

    public DateTimeOffset AtUtc { get; private set; }

    private MemberLog()
    {
    }

    public static MemberLog Record(Guid customerId, string eventName, string? details = null) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        Event = eventName.Trim(),
        Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
        AtUtc = DateTimeOffset.UtcNow,
    };
}
