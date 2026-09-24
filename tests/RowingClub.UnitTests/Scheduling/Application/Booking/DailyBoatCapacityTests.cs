using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Scheduling.Application.Availability;
using RowingClub.Scheduling.Application.Booking;
using RowingClub.Scheduling.Application.Rsvp;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Consents;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Instructors;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Sessions;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.UnitTests.Scheduling.Application.Booking;

public sealed class DailyBoatCapacityTests
{
    private static readonly TimeOnly OneOClock = new(13, 0);
    private static readonly TimeOnly TwoOClock = new(14, 0);

    private readonly ISettingsRepository _settingsRepository = Substitute.For<ISettingsRepository>();
    private readonly IClosedDateRepository _closedDateRepository = Substitute.For<IClosedDateRepository>();
    private readonly ITrainingSessionRepository _sessionRepository = Substitute.For<ITrainingSessionRepository>();
    private readonly IBoatRepository _boatRepository = Substitute.For<IBoatRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IConsentRecordRepository _consentRepository = Substitute.For<IConsentRecordRepository>();
    private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
    private readonly IInstructorRepository _instructorRepository = Substitute.For<IInstructorRepository>();
    private readonly ICustomerPackageRepository _customerPackageRepository = Substitute.For<ICustomerPackageRepository>();
    private readonly IMemberLogRepository _memberLogRepository = Substitute.For<IMemberLogRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();
    private readonly IOpaqueTokenGenerator _tokenGenerator = Substitute.For<IOpaqueTokenGenerator>();
    private readonly IRefreshTokenHasher _tokenHasher = Substitute.For<IRefreshTokenHasher>();
    private readonly IAppointmentRsvpEmailSender _rsvpEmailSender = Substitute.For<IAppointmentRsvpEmailSender>();

    private readonly DateOnly _date = OpenDateWithSlots(OneOClock, TwoOClock);

    public DailyBoatCapacityTests()
    {
        _tokenGenerator.Generate().Returns("raw-token");
        _tokenHasher.Hash(Arg.Any<string>()).Returns("hashed-token");
        _instructorRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Instructor>());
    }

    private static DateOnly OpenDateWithSlots(params TimeOnly[] slots)
    {
        var settings = CompanySettings.Default();
        var today = DateOnly.FromDateTime(settings.NowLocal());
        for (var offset = 3; offset < 20; offset++)
        {
            var candidate = today.AddDays(offset);
            var daySlots = settings.Slots(candidate.DayOfWeek).ToList();
            if (slots.All(daySlots.Contains))
                return candidate;
        }

        throw new InvalidOperationException("Test için uygun gün bulunamadı.");
    }

    private List<Boat> GivenQuadBoats(int count)
    {
        var boats = Enumerable.Range(1, count).Select(i => Boat.Create($"Dörtlü {i}", BoatClass.Quad4x)).ToList();
        _boatRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(boats);
        return boats;
    }

    private TrainingSession GivenSessionWithOneMember(Boat boat, TimeOnly slot)
    {
        var session = TrainingSession.Create(_date, slot, BoatClass.Quad4x, 0, boat.Id, null);
        session.AssignBoat(boat);
        Appointment.Book(Customer.Create("Veli Demir", "+905550000001", null), session, null, null, null);
        _sessionRepository.GetByDateAsync(_date, Arg.Any<CancellationToken>()).Returns([session]);
        return session;
    }

    private async Task<Dictionary<string, int>> SeatsByTimeAsync()
    {
        var handler = new GetAvailabilityQueryHandler(
            _settingsRepository, _closedDateRepository, _sessionRepository, _boatRepository, _customerRepository);
        var slots = await handler.Handle(new GetAvailabilityQuery(_date, "4x", null), CancellationToken.None);
        return slots.ToDictionary(s => s.Time, s => s.SeatsLeft);
    }

    private BookAppointmentCommandHandler CreateBookingHandler() => new(
        _settingsRepository, _closedDateRepository, _consentRepository, _customerRepository,
        _appointmentRepository, _sessionRepository, _boatRepository, _instructorRepository,
        _customerPackageRepository, _memberLogRepository, _unitOfWork,
        _tokenGenerator, _tokenHasher, _rsvpEmailSender, NullLogger<BookAppointmentCommandHandler>.Instance);

    private BookAppointmentCommand GuestBooking(TimeOnly slot) => new(
        "Ada Lovelace", "+905551112233", "ada@example.com", _date, slot, "4x",
        ExperienceAcknowledged: true, TeammateName: null, Note: null, ReminderMinutes: 0, UsePackage: false,
        ConsentCatalog.All.Where(c => c.Scope == ConsentScope.Booking).Select(c => c.Key).ToList(), "127.0.0.1",
        "Kürek Kulübü", "kurek");

    [Fact]
    public async Task One_boat_used_at_one_o_clock_leaves_no_seats_at_other_hours()
    {
        var boats = GivenQuadBoats(1);
        GivenSessionWithOneMember(boats[0], OneOClock);

        var seats = await SeatsByTimeAsync();

        seats["13:00"].Should().Be(3);
        seats.Where(s => s.Key != "13:00").Should().OnlyContain(s => s.Value == 0);
    }

    [Fact]
    public async Task Second_boat_adds_a_full_boat_of_seats_to_every_hour()
    {
        var boats = GivenQuadBoats(2);
        GivenSessionWithOneMember(boats[0], OneOClock);

        var seats = await SeatsByTimeAsync();

        seats["13:00"].Should().Be(7);
        seats.Where(s => s.Key != "13:00").Should().OnlyContain(s => s.Value == 4);
    }

    [Fact]
    public async Task Booking_another_hour_is_refused_when_the_only_boat_is_used_that_day()
    {
        var boats = GivenQuadBoats(1);
        GivenSessionWithOneMember(boats[0], OneOClock);

        var act = () => CreateBookingHandler().Handle(GuestBooking(TwoOClock), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.ErrorCode.Should().Be("slot_full");
        _sessionRepository.DidNotReceive().Add(Arg.Any<TrainingSession>());
    }

    [Fact]
    public async Task Booking_the_same_hour_joins_the_existing_session_and_opens_an_rsvp_window()
    {
        var boats = GivenQuadBoats(1);
        var session = GivenSessionWithOneMember(boats[0], OneOClock);

        var response = await CreateBookingHandler().Handle(GuestBooking(OneOClock), CancellationToken.None);

        response.SessionId.Should().Be(session.Id);
        response.Status.Should().Be("Pending");
        response.RsvpDeadlineUtc.Should().NotBeNull();
        session.ActiveMemberCount.Should().Be(2);
        await _rsvpEmailSender.Received(1).SendAsync(
            "ada@example.com", "Ada", "Kürek Kulübü", _date, OneOClock, Arg.Any<string>(), "raw-token", "kurek",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rsvp_email_failure_does_not_fail_the_booking()
    {
        var boats = GivenQuadBoats(1);
        GivenSessionWithOneMember(boats[0], OneOClock);
        _rsvpEmailSender
            .SendAsync(default!, default!, default!, default, default, default!, default!, default!, default)
            .ReturnsForAnyArgs<Task>(_ => throw new InvalidOperationException("smtp down"));

        var response = await CreateBookingHandler().Handle(GuestBooking(OneOClock), CancellationToken.None);

        response.Status.Should().Be("Pending");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Booking_opens_a_new_session_on_a_boat_not_used_that_day()
    {
        var boats = GivenQuadBoats(2);
        GivenSessionWithOneMember(boats[0], OneOClock);

        var response = await CreateBookingHandler().Handle(GuestBooking(TwoOClock), CancellationToken.None);

        response.BoatName.Should().Be(boats[1].Name);
        _sessionRepository.Received(1).Add(Arg.Is<TrainingSession>(s => s!.BoatId == boats[1].Id && s.StartTime == TwoOClock));
    }
}
