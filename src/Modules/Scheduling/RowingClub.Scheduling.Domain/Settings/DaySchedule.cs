using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Settings;

/// <summary>Bir haftanın günü için çalışma saati. <see cref="CompanySettings"/> her gün için tam olarak bir tane taşır.</summary>
public sealed class DaySchedule
{
    public Guid Id { get; private set; }

    public Guid CompanySettingsId { get; private set; }

    public DayOfWeek Day { get; private set; }

    public bool IsOpen { get; private set; }

    public TimeOnly OpeningTime { get; private set; }

    public TimeOnly ClosingTime { get; private set; }

    private DaySchedule()
    {
    }

    public static DaySchedule Create(
        Guid companySettingsId, DayOfWeek day, bool isOpen, TimeOnly openingTime, TimeOnly closingTime)
    {
        if (isOpen && closingTime <= openingTime)
            throw new DomainException("invalid_hours", "Kapanış saati açılış saatinden sonra olmalıdır.");

        return new DaySchedule
        {
            Id = Guid.NewGuid(),
            CompanySettingsId = companySettingsId,
            Day = day,
            IsOpen = isOpen,
            OpeningTime = openingTime,
            ClosingTime = closingTime,
        };
    }
}
