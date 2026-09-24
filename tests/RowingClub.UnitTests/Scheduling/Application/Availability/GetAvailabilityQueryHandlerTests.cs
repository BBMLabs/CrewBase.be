using FluentAssertions;
using NSubstitute;
using RowingClub.Scheduling.Application.Availability;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Sessions;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.UnitTests.Scheduling.Application.Availability;

public sealed class GetAvailabilityQueryHandlerTests
{
    private readonly ISettingsRepository _settingsRepository = Substitute.For<ISettingsRepository>();
    private readonly IClosedDateRepository _closedDateRepository = Substitute.For<IClosedDateRepository>();
    private readonly ITrainingSessionRepository _sessionRepository = Substitute.For<ITrainingSessionRepository>();
    private readonly IBoatRepository _boatRepository = Substitute.For<IBoatRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();

    private readonly DateOnly _date;
    private readonly TimeOnly _slot;

    public GetAvailabilityQueryHandlerTests()
    {
        // Varsayılan ayarlarda açık olan, minimum ön bildirim süresinin ötesindeki ilk gün.
        var settings = CompanySettings.Default();
        _date = Enumerable.Range(3, 7)
            .Select(d => DateOnly.FromDateTime(DateTime.Today).AddDays(d))
            .First(d => settings.Slots(d.DayOfWeek).Any());
        _slot = settings.Slots(_date.DayOfWeek).First();

        _settingsRepository.GetAsync(Arg.Any<CancellationToken>()).Returns((CompanySettings?)null);
        _sessionRepository.GetByDateAsync(_date, Arg.Any<CancellationToken>()).Returns(new List<TrainingSession>());
        _boatRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Boat> { Boat.Create("Çift 1", BoatClass.Double2x) });
    }

    private GetAvailabilityQueryHandler CreateHandler() =>
        new(_settingsRepository, _closedDateRepository, _sessionRepository, _boatRepository, _customerRepository);

    private async Task<SlotDto> SlotFor(string? phone) =>
        (await CreateHandler().Handle(new GetAvailabilityQuery(_date, "2x", phone), CancellationToken.None))
            .Single(s => s.Time == _slot.ToString("HH:mm"));

    [Fact]
    public async Task Guest_sees_no_2x_seats_when_only_a_new_session_could_be_opened()
    {
        // Regresyon: boş 2x tekne varken misafire koltuk gösteriliyor, rezervasyon ise
        // no_2x_partner_available ile reddediliyordu.
        var slot = await SlotFor(phone: null);

        slot.Available.Should().BeFalse();
        slot.SeatsLeft.Should().Be(0);
    }

    [Fact]
    public async Task Guest_can_join_an_existing_2x_session_with_a_free_seat_at_any_level()
    {
        var session = TrainingSession.Create(_date, _slot, BoatClass.Double2x, level: 3, null, null);
        Appointment.Book(Customer.Create("Ada Lovelace", "+905551112233", "ada@example.com"), session, null, null, null);
        _sessionRepository.GetByDateAsync(_date, Arg.Any<CancellationToken>()).Returns(new List<TrainingSession> { session });

        var slot = await SlotFor(phone: null);

        slot.Available.Should().BeTrue();
        slot.SeatsLeft.Should().Be(session.Capacity - 1);
    }

    [Fact]
    public async Task Account_holder_can_open_a_new_2x_session_on_a_free_boat()
    {
        var member = Customer.Create("Grace Hopper", "+905559998877", "grace@example.com");
        member.AttachAccount("grace@example.com", "hash");
        _customerRepository.GetByPhoneAsync("+905559998877", Arg.Any<CancellationToken>()).Returns(member);

        var slot = await SlotFor(phone: "+905559998877");

        slot.Available.Should().BeTrue();
        slot.SeatsLeft.Should().Be(BoatClass.Double2x.Capacity());
    }
}
