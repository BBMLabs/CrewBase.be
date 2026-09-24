using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Scheduling.Application.Members;
using RowingClub.Scheduling.Application.Rsvp;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Sessions;

namespace RowingClub.UnitTests.Scheduling.Application.Rsvp;

public sealed class RsvpHandlerTests
{
    private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
    private readonly ICustomerPackageRepository _customerPackageRepository = Substitute.For<ICustomerPackageRepository>();
    private readonly IMemberLogRepository _memberLogRepository = Substitute.For<IMemberLogRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();
    private readonly IRefreshTokenHasher _tokenHasher = Substitute.For<IRefreshTokenHasher>();
    private readonly List<MemberLog> _logs = [];

    public RsvpHandlerTests()
    {
        _tokenHasher.Hash(Arg.Any<string>()).Returns(ci => "h:" + ci.Arg<string>());
        _memberLogRepository.When(r => r.Add(Arg.Any<MemberLog>())).Do(ci => _logs.Add(ci.Arg<MemberLog>()!));
    }

    private ProcessRsvpDeadlinesCommandHandler CreateProcessor() =>
        new(_appointmentRepository, _customerPackageRepository, _memberLogRepository, _unitOfWork);

    private (Appointment Appointment, CustomerPackage Package) CreatePackageBooking(DateTimeOffset bookedAt)
    {
        var customer = Customer.Create("Ada Lovelace", "+905551112233", "ada@example.com");
        var package = CustomerPackage.Assign(
            customer.Id, LessonPackage.Create("8 Ders", null, 8, 100m, null), CustomerPackageSource.Assigned);
        package.Deduct();
        var session = TrainingSession.Create(
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)), new TimeOnly(9, 0), BoatClass.Quad4x, 0, null, null);
        var appointment = Appointment.Book(customer, session, null, null, package.Id);
        appointment.OpenRsvp("h:token", bookedAt);

        _customerPackageRepository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);
        _appointmentRepository.GetByRsvpTokenHashAsync("h:token", Arg.Any<CancellationToken>()).Returns(appointment);
        return (appointment, package);
    }

    private void ReturnDue(params Appointment[] appointments) =>
        _appointmentRepository.GetDueRsvpsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(appointments.ToList());

    [Fact]
    public async Task Not_attending_cancels_refunds_the_package_and_logs_the_rsvp_cancellation()
    {
        var bookedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var (appointment, package) = CreatePackageBooking(bookedAt);
        appointment.ChooseRsvp(RsvpChoice.NotAttending, bookedAt.AddMinutes(10));
        ReturnDue(appointment);

        var result = await CreateProcessor().Handle(new ProcessRsvpDeadlinesCommand(), CancellationToken.None);

        result.Cancelled.Should().Be(1);
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        package.RemainingSessions.Should().Be(8);
        _logs.Select(l => l.Event).Should().BeEquivalentTo(
            [MemberEvents.PackageRefunded, MemberEvents.AppointmentCancelled, MemberEvents.AppointmentCancelledByRsvp]);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rsvp_cancellation_ignores_the_member_24_hour_cancellation_rule()
    {
        var bookedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var customer = Customer.Create("Ada Lovelace", "+905551112233", "ada@example.com");
        var session = TrainingSession.Create(
            DateOnly.FromDateTime(DateTime.Today), new TimeOnly(0, 0), BoatClass.Quad4x, 0, null, null);
        var appointment = Appointment.Book(customer, session, null, null, null);
        appointment.OpenRsvp("h:other", bookedAt);
        appointment.ChooseRsvp(RsvpChoice.NotAttending, bookedAt.AddMinutes(1));
        ReturnDue(appointment);

        await CreateProcessor().Handle(new ProcessRsvpDeadlinesCommand(), CancellationToken.None);

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Theory]
    [InlineData(RsvpChoice.None)]
    [InlineData(RsvpChoice.Attending)]
    public async Task No_answer_or_attending_confirms_without_touching_the_package(RsvpChoice choice)
    {
        var bookedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var (appointment, package) = CreatePackageBooking(bookedAt);
        if (choice != RsvpChoice.None)
            appointment.ChooseRsvp(choice, bookedAt.AddMinutes(10));
        ReturnDue(appointment);

        var result = await CreateProcessor().Handle(new ProcessRsvpDeadlinesCommand(), CancellationToken.None);

        result.Confirmed.Should().Be(1);
        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
        package.RemainingSessions.Should().Be(7);
        _logs.Should().BeEmpty();
    }

    [Fact]
    public async Task Staff_changed_status_is_left_untouched()
    {
        var bookedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var (appointment, package) = CreatePackageBooking(bookedAt);
        appointment.ChooseRsvp(RsvpChoice.NotAttending, bookedAt.AddMinutes(10));
        appointment.SetStatus(AppointmentStatus.Confirmed);
        ReturnDue(appointment);

        var result = await CreateProcessor().Handle(new ProcessRsvpDeadlinesCommand(), CancellationToken.None);

        result.Cancelled.Should().Be(0);
        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
        package.RemainingSessions.Should().Be(7);
    }

    [Fact]
    public async Task Running_twice_does_not_refund_twice()
    {
        var bookedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var (appointment, package) = CreatePackageBooking(bookedAt);
        package.Deduct();
        appointment.ChooseRsvp(RsvpChoice.NotAttending, bookedAt.AddMinutes(10));
        ReturnDue(appointment);
        var processor = CreateProcessor();

        await processor.Handle(new ProcessRsvpDeadlinesCommand(), CancellationToken.None);
        var second = await processor.Handle(new ProcessRsvpDeadlinesCommand(), CancellationToken.None);

        second.Cancelled.Should().Be(0);
        package.RemainingSessions.Should().Be(7);
    }

    [Fact]
    public async Task Submit_within_the_window_saves_the_choice_and_keeps_pending()
    {
        var (appointment, _) = CreatePackageBooking(DateTimeOffset.UtcNow);
        var handler = new SubmitRsvpCommandHandler(_appointmentRepository, _tokenHasher, _unitOfWork);

        var result = await handler.Handle(new SubmitRsvpCommand("token", "notAttending", "Kulüp"), CancellationToken.None);

        result.Outcome.Should().Be(RsvpSubmitOutcome.Saved);
        result.Rsvp!.Choice.Should().Be("notAttending");
        result.Rsvp.Status.Should().Be("Pending");
        result.Rsvp.Open.Should().BeTrue();
        appointment.RsvpChoice.Should().Be(RsvpChoice.NotAttending);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_after_the_deadline_is_closed()
    {
        var (appointment, _) = CreatePackageBooking(DateTimeOffset.UtcNow.AddHours(-2));
        var handler = new SubmitRsvpCommandHandler(_appointmentRepository, _tokenHasher, _unitOfWork);

        var result = await handler.Handle(new SubmitRsvpCommand("token", "attending", "Kulüp"), CancellationToken.None);

        result.Outcome.Should().Be(RsvpSubmitOutcome.Closed);
        appointment.RsvpChoice.Should().Be(RsvpChoice.None);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("maybe")]
    [InlineData("none")]
    [InlineData("")]
    public async Task Submit_with_an_invalid_choice_is_rejected(string choice)
    {
        CreatePackageBooking(DateTimeOffset.UtcNow);
        var handler = new SubmitRsvpCommandHandler(_appointmentRepository, _tokenHasher, _unitOfWork);

        var result = await handler.Handle(new SubmitRsvpCommand("token", choice, "Kulüp"), CancellationToken.None);

        result.Outcome.Should().Be(RsvpSubmitOutcome.InvalidChoice);
    }

    [Fact]
    public async Task Unknown_token_is_not_found()
    {
        var handler = new SubmitRsvpCommandHandler(_appointmentRepository, _tokenHasher, _unitOfWork);

        var result = await handler.Handle(new SubmitRsvpCommand("nope", "attending", "Kulüp"), CancellationToken.None);

        result.Outcome.Should().Be(RsvpSubmitOutcome.NotFound);
    }
}
