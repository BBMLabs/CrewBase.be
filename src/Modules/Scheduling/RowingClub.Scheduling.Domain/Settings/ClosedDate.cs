namespace RowingClub.Scheduling.Domain.Settings;

/// <summary>
/// Firmanın randevuya kapattığı tekil tarih (bayram, bakım günü...). Haftalık açık günlerin
/// (CompanySettings.OpenDaysMask) üzerine istisna olarak uygulanır.
/// </summary>
public sealed class ClosedDate
{
    public Guid Id { get; private set; }

    public DateOnly Date { get; private set; }

    public string? Reason { get; private set; }

    private ClosedDate()
    {
    }

    public static ClosedDate Create(DateOnly date, string? reason) => new()
    {
        Id = Guid.NewGuid(),
        Date = date,
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
    };
}
