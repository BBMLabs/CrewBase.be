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
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var customer = await customerRepository.GetByPhoneAsync(request.Phone, cancellationToken);
            level = customer?.Level ?? 0;
        }

        var daySessions = await sessionRepository.GetByDateAsync(request.Date, cancellationToken);
        var boatsOfClass = (await boatRepository.GetAllAsync(cancellationToken))
            .Where(b => b.IsActive && b.Class == boatClass)
            .ToList();

        var slots = new List<SlotDto>();
        foreach (var slot in settings.Slots(request.Date.DayOfWeek))
        {
            var seatsLeft = SeatsLeftAt(daySessions, boatsOfClass, boatClass, level, slot);
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

    private static int SeatsLeftAt(
        List<TrainingSession> daySessions, List<Boat> boatsOfClass,
        BoatClass boatClass, int level, TimeOnly slot)
    {
        var slotSessions = daySessions.Where(s => s.StartTime == slot).ToList();

        // 1) Katılınabilir mevcut seans: aynı sınıf + aynı derece + boş koltuk.
        var joinableSeats = slotSessions
            .Where(s => s.BoatClass == boatClass && s.Level == level && s.HasFreeSeat)
            .Sum(s => s.Capacity - s.ActiveMemberCount);

        // 2) Yeni seans açma imkânı: sınıfta boş tekne (ya da hiç tekne tanımlı değilse 1x serbest).
        int newSessionSeats;
        if (boatsOfClass.Count > 0)
        {
            var usedBoatIds = slotSessions.Where(s => s.BoatId is not null).Select(s => s.BoatId!.Value).ToHashSet();
            var freeBoats = boatsOfClass.Count(b => !usedBoatIds.Contains(b.Id));
            newSessionSeats = freeBoats * boatClass.Capacity();
        }
        else
        {
            newSessionSeats = boatClass == BoatClass.Single1x ? 1 : 0;
        }

        return joinableSeats + newSessionSeats;
    }
}
