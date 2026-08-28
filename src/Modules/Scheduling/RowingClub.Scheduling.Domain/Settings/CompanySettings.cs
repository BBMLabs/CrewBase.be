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

    private readonly List<DaySchedule> _daySchedules = new();

    /// <summary>Haftanın 7 günü için ayrı çalışma saatleri (Pazar=0 ... Cumartesi=6).</summary>
    public IReadOnlyList<DaySchedule> DaySchedules => _daySchedules;

    /// <summary>Slot uzunluğu dakika cinsinden (ders süresi).</summary>
    public int SlotMinutes { get; private set; }

    /// <summary>Randevu en geç, ders başlangıcından bu kadar saat önce alınabilir.</summary>
    public int MinNoticeHours { get; private set; }

    /// <summary>Randevu en erken, bugünden bu kadar gün sonrası için alınabilir.</summary>
    public int MaxAdvanceDays { get; private set; }

    /// <summary>Kullanıcılara sunulan hatırlatma seçenekleri, dakika cinsinden CSV: "60,120,1440".</summary>
    public string ReminderOptionsMinutes { get; private set; } = null!;

    public int DefaultReminderMinutes { get; private set; }

    /// <summary>Ders paketi süresi dolmadan kaç gün kala hatırlatma gönderilsin, CSV: "15,7". Admin panelden değiştirir.</summary>
    public string PackageExpiryReminderDaysCsv { get; private set; } = null!;

    /// <summary>Çalışma saatleri ve kısıtlar bu saat dilimindeki yerel saate göre yorumlanır.</summary>
    public string TimeZoneId { get; private set; } = null!;

    /// <summary>Yeni randevu oluşunca firma personeline bildirim gönderilsin mi.</summary>
    public bool NotifyOnNewAppointment { get; private set; } = true;

    /// <summary>Randevu iptalinde firma personeline bildirim gönderilsin mi.</summary>
    public bool NotifyOnCancellation { get; private set; } = true;

    /// <summary>Üyelere/misafirlere randevu hatırlatması gönderilsin mi.</summary>
    public bool SendCustomerReminders { get; private set; } = true;

    private CompanySettings()
    {
    }

    public static CompanySettings Default()
    {
        var settings = new CompanySettings
        {
            Id = Guid.NewGuid(),
            SlotMinutes = 60,
            MinNoticeHours = 2,
            MaxAdvanceDays = 30,
            ReminderOptionsMinutes = "60,120,1440",
            DefaultReminderMinutes = 120,
            PackageExpiryReminderDaysCsv = "15,7",
            TimeZoneId = "Europe/Istanbul",
            NotifyOnNewAppointment = true,
            NotifyOnCancellation = true,
            SendCustomerReminders = true,
        };

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            settings._daySchedules.Add(DaySchedule.Create(settings.Id, day, true, new TimeOnly(9, 0), new TimeOnly(18, 0)));

        return settings;
    }

    public void UpdateWorkingHours(
        IReadOnlyCollection<(DayOfWeek Day, bool IsOpen, TimeOnly OpeningTime, TimeOnly ClosingTime)> days,
        int slotMinutes)
    {
        if (days.Count != 7 || days.Select(d => d.Day).Distinct().Count() != 7)
            throw new DomainException("invalid_days", "Haftanın 7 günü için de çalışma saati tanımlanmalıdır.");

        if (!days.Any(d => d.IsOpen))
            throw new DomainException("no_open_days", "En az bir gün açık olmalıdır.");

        if (slotMinutes is < 15 or > 240)
            throw new DomainException("invalid_slot_length", "Slot süresi 15-240 dakika arasında olmalıdır.");

        _daySchedules.Clear();
        foreach (var d in days)
            _daySchedules.Add(DaySchedule.Create(Id, d.Day, d.IsOpen, d.OpeningTime, d.ClosingTime));

        SlotMinutes = slotMinutes;
    }

    public void UpdateBookingRules(
        int minNoticeHours, int maxAdvanceDays, IReadOnlyCollection<int> reminderOptionsMinutes,
        int defaultReminderMinutes, string timeZoneId)
    {
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

        MinNoticeHours = minNoticeHours;
        MaxAdvanceDays = maxAdvanceDays;
        ReminderOptionsMinutes = string.Join(',', reminderOptionsMinutes.Distinct().OrderBy(m => m));
        DefaultReminderMinutes = defaultReminderMinutes;
        TimeZoneId = timeZoneId;
    }

    public void UpdatePackageExpirySettings(IReadOnlyCollection<int> reminderDaysBeforeExpiry)
    {
        if (reminderDaysBeforeExpiry.Count == 0 || reminderDaysBeforeExpiry.Any(d => d < 1))
            throw new DomainException("invalid_package_reminder_days", "En az bir geçerli hatırlatma günü tanımlanmalıdır.");

        PackageExpiryReminderDaysCsv = string.Join(',', reminderDaysBeforeExpiry.Distinct().OrderByDescending(d => d));
    }

    public void UpdateNotifications(bool notifyOnNewAppointment, bool notifyOnCancellation, bool sendCustomerReminders)
    {
        NotifyOnNewAppointment = notifyOnNewAppointment;
        NotifyOnCancellation = notifyOnCancellation;
        SendCustomerReminders = sendCustomerReminders;
    }

    public DaySchedule ScheduleFor(DayOfWeek day) => _daySchedules.First(d => d.Day == day);

    public bool IsOpenOn(DayOfWeek day) => ScheduleFor(day).IsOpen;

    public IEnumerable<TimeOnly> Slots(DayOfWeek day)
    {
        var schedule = ScheduleFor(day);
        if (!schedule.IsOpen)
            yield break;

        for (var slot = schedule.OpeningTime; slot.AddMinutes(SlotMinutes, out var wrapped) <= schedule.ClosingTime && wrapped == 0;
             slot = slot.AddMinutes(SlotMinutes))
        {
            yield return slot;
        }
    }

    public bool IsValidSlot(DateOnly date, TimeOnly time) => Slots(date.DayOfWeek).Contains(time);

    /// <summary>Büyükten küçüğe sıralı (bkz. UpdatePackageExpirySettings) - ProcessPackageExpiriesCommand bu sırada gezer.</summary>
    public IReadOnlyList<int> PackageExpiryReminderDays() =>
        PackageExpiryReminderDaysCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToList();

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
        var schedule = ScheduleFor(date.DayOfWeek);
        if (!schedule.IsOpen)
            throw new DomainException("closed_day", "Seçtiğiniz gün firma kapalıdır.");

        if (!Slots(date.DayOfWeek).Contains(time))
            throw new DomainException(
                "invalid_slot",
                $"Randevu saati {schedule.OpeningTime:HH\\:mm}-{schedule.ClosingTime:HH\\:mm} arasında {SlotMinutes} dakikalık dilimlere denk gelmelidir.");

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
