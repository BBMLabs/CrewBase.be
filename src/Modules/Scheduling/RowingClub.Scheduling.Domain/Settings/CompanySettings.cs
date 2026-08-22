using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Settings;

/// <summary>
/// Firmanın kendi belirlediği çalışma düzeni ve randevu kuralları. Tenant veritabanında tek satır
/// olarak yaşar; firma provision edilirken <see cref="Default"/> ile tohumlanır ve panelden
/// güncellenir. Eski statik BookingRules'un yerini alır - artık her firma kendi kurallarını koyar.
/// </summary>
public sealed class CompanySettings
{
    public Guid Id { get; private set; }

    public TimeOnly OpeningTime { get; private set; }

    public TimeOnly ClosingTime { get; private set; }

    /// <summary>Slot uzunluğu dakika cinsinden (ders süresi).</summary>
    public int SlotMinutes { get; private set; }

    /// <summary>Açık günler bit maskesi: 1 &lt;&lt; (int)DayOfWeek (Pazar=0 ... Cumartesi=6).</summary>
    public int OpenDaysMask { get; private set; }

    /// <summary>Randevu en geç, ders başlangıcından bu kadar saat önce alınabilir.</summary>
    public int MinNoticeHours { get; private set; }

    /// <summary>Randevu en erken, bugünden bu kadar gün sonrası için alınabilir.</summary>
    public int MaxAdvanceDays { get; private set; }

    /// <summary>Kullanıcılara sunulan hatırlatma seçenekleri, dakika cinsinden CSV: "60,120,1440".</summary>
    public string ReminderOptionsMinutes { get; private set; } = null!;

    public int DefaultReminderMinutes { get; private set; }

    /// <summary>Çalışma saatleri ve kısıtlar bu saat dilimindeki yerel saate göre yorumlanır.</summary>
    public string TimeZoneId { get; private set; } = null!;

    private CompanySettings()
    {
    }

    public static CompanySettings Default() => new()
    {
        Id = Guid.NewGuid(),
        OpeningTime = new TimeOnly(9, 0),
        ClosingTime = new TimeOnly(18, 0),
        SlotMinutes = 60,
        OpenDaysMask = 0b1111111, // her gün açık
        MinNoticeHours = 2,
        MaxAdvanceDays = 30,
        ReminderOptionsMinutes = "60,120,1440",
        DefaultReminderMinutes = 120,
        TimeZoneId = "Europe/Istanbul",
    };

    public void Update(
        TimeOnly openingTime, TimeOnly closingTime, int slotMinutes, int openDaysMask,
        int minNoticeHours, int maxAdvanceDays, IReadOnlyCollection<int> reminderOptionsMinutes,
        int defaultReminderMinutes, string timeZoneId)
    {
        if (closingTime <= openingTime)
            throw new DomainException("invalid_hours", "Kapanış saati açılış saatinden sonra olmalıdır.");

        if (slotMinutes is < 15 or > 240)
            throw new DomainException("invalid_slot_length", "Slot süresi 15-240 dakika arasında olmalıdır.");

        if ((openDaysMask & 0b1111111) == 0)
            throw new DomainException("no_open_days", "En az bir gün açık olmalıdır.");

        if (minNoticeHours < 0 || maxAdvanceDays < 1)
            throw new DomainException("invalid_booking_window", "Randevu kısıtları geçersiz.");

        if (reminderOptionsMinutes.Count == 0 || reminderOptionsMinutes.Any(m => m < 5))
            throw new DomainException("invalid_reminder_options", "En az bir geçerli hatırlatma seçeneği tanımlanmalıdır.");

        if (!reminderOptionsMinutes.Contains(defaultReminderMinutes))
            throw new DomainException("invalid_default_reminder", "Varsayılan hatırlatma, seçeneklerden biri olmalıdır.");

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new DomainException("invalid_timezone", "Geçersiz saat dilimi.");
        }

        OpeningTime = openingTime;
        ClosingTime = closingTime;
        SlotMinutes = slotMinutes;
        OpenDaysMask = openDaysMask & 0b1111111;
        MinNoticeHours = minNoticeHours;
        MaxAdvanceDays = maxAdvanceDays;
        ReminderOptionsMinutes = string.Join(',', reminderOptionsMinutes.Distinct().OrderBy(m => m));
        DefaultReminderMinutes = defaultReminderMinutes;
        TimeZoneId = timeZoneId;
    }

    public bool IsOpenOn(DayOfWeek day) => (OpenDaysMask & (1 << (int)day)) != 0;

    public IEnumerable<TimeOnly> Slots()
    {
        for (var slot = OpeningTime; slot.AddMinutes(SlotMinutes, out var wrapped) <= ClosingTime && wrapped == 0;
             slot = slot.AddMinutes(SlotMinutes))
        {
            yield return slot;
        }
    }

    public bool IsValidSlot(DateOnly date, TimeOnly time) =>
        IsOpenOn(date.DayOfWeek) && Slots().Contains(time);

    public IReadOnlyList<int> ReminderOptions() =>
        ReminderOptionsMinutes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToList();

    public DateTime NowLocal()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
    }

    /// <summary>Slot geçerliliği + min/max rezervasyon penceresi; ihlalde DomainException fırlatır.</summary>
    public void EnsureBookable(DateOnly date, TimeOnly time)
    {
        if (!IsOpenOn(date.DayOfWeek))
            throw new DomainException("closed_day", "Seçtiğiniz gün firma kapalıdır.");

        if (!Slots().Contains(time))
            throw new DomainException(
                "invalid_slot",
                $"Randevu saati {OpeningTime:HH\\:mm}-{ClosingTime:HH\\:mm} arasında {SlotMinutes} dakikalık dilimlere denk gelmelidir.");

        var nowLocal = NowLocal();
        var startsAt = date.ToDateTime(time);

        if (startsAt < nowLocal.AddHours(MinNoticeHours))
            throw new DomainException(
                "too_soon", $"Randevular ders başlangıcından en az {MinNoticeHours} saat önce alınabilir.");

        if (date > DateOnly.FromDateTime(nowLocal).AddDays(MaxAdvanceDays))
            throw new DomainException(
                "too_far", $"En fazla {MaxAdvanceDays} gün sonrası için randevu alınabilir.");
    }
}
