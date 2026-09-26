using MediatR;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Sessions;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Application.Availability;

/// <summary>
/// Bir gün için slot durumu. Telefon verilirse üyenin derecesine göre (katılabileceği seanslar),
/// verilmezse derecesiz (0) kabul edilerek hesaplanır.
/// </summary>
public sealed record GetAvailabilityQuery(DateOnly Date, string BoatClass, string? Phone)
    : IRequest<List<SlotDto>>;

public sealed record SlotDto(string Time, bool Available, int SeatsLeft);

public sealed class GetAvailabilityQueryHandler(
    ISettingsRepository settingsRepository,
    IClosedDateRepository closedDateRepository,
    ITrainingSessionRepository sessionRepository,
    IBoatRepository boatRepository,
    ICustomerRepository customerRepository)
    : IRequestHandler<GetAvailabilityQuery, List<SlotDto>>
{
    public async Task<List<SlotDto>> Handle(GetAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken) ?? CompanySettings.Default();
        var boatClass = BoatClassExtensions.Parse(request.BoatClass);

        if (await closedDateRepository.IsClosedAsync(request.Date, cancellationToken))
        {
            return settings.Slots(request.Date.DayOfWeek).Select(s => new SlotDto(s.ToString("HH:mm"), false, 0)).ToList();
        }

        var level = 0;
        var hasAccount = false;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var customer = await customerRepository.GetByPhoneAsync(request.Phone, cancellationToken);
            level = customer?.Level ?? 0;
            hasAccount = customer?.HasAccount ?? false;
        }

        // BookAppointmentCommandHandler ile aynı kural: hesabı olmayan misafir yeni bir 2x seans açamaz,
        // yalnızca o saatte boş koltuğu olan mevcut bir 2x seansa (seviyeden bağımsız) katılabilir.
        // Aksi halde müsaitlik slotu boş gösterir, rezervasyon no_2x_partner_available ile reddedilirdi.
        var joinExistingOnly = !hasAccount && boatClass == BoatClass.Double2x;

        var daySessions = await sessionRepository.GetByDateAsync(request.Date, cancellationToken);
        var boatsOfClass = (await boatRepository.GetAllAsync(cancellationToken))
            .Where(b => b.IsActive && b.Class == boatClass)
            .ToList();

        var slots = new List<SlotDto>();
        foreach (var slot in settings.Slots(request.Date.DayOfWeek))
        {
            var seatsLeft = joinExistingOnly
                ? JoinableSeatsAnyLevel(daySessions, boatClass, slot)
                : DailyBoatCapacity.SeatsLeftAt(daySessions, boatClass, boatsOfClass.Count, slot);
            var bookable = seatsLeft > 0 && IsBookable(settings, request.Date, slot);
            slots.Add(new SlotDto(slot.ToString("HH:mm"), bookable, seatsLeft));
        }

        return slots;
    }

    private static bool IsBookable(CompanySettings settings, DateOnly date, TimeOnly time)
    {
        try
        {
            settings.EnsureBookable(date, time);
            return true;
        }
        catch (RowingClub.BuildingBlocks.Domain.DomainException)
        {
            return false;
        }
    }

    private static int JoinableSeatsAnyLevel(List<TrainingSession> daySessions, BoatClass boatClass, TimeOnly slot) =>
        daySessions
            .Where(s => s.StartTime == slot && s.BoatClass == boatClass && s.HasFreeSeat && DailyBoatCapacity.HoldsBoat(s))
            .Sum(s => s.Capacity - s.ActiveMemberCount);
}
