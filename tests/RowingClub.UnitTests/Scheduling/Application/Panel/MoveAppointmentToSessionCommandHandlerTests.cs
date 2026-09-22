using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Panel;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Sessions;

namespace RowingClub.UnitTests.Scheduling.Application.Panel;

public sealed class MoveAppointmentToSessionCommandHandlerTests
{
    private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
    private readonly ITrainingSessionRepository _sessionRepository = Substitute.For<ITrainingSessionRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private MoveAppointmentToSessionCommandHandler CreateHandler() =>
        new(_appointmentRepository, _sessionRepository, _unitOfWork);

    private static TrainingSession CreateSession(BoatClass boatClass, int level = 0) =>
        TrainingSession.Create(
            DateOnly.FromDateTime(DateTime.Today), new TimeOnly(9, 0), boatClass, level, null, null);

    private static Appointment CreateAppointment(TrainingSession session)
    {
        var customer = Customer.Create("Ada Lovelace", "+905551112233", "ada@example.com");
        return Appointment.Book(customer, session, null, null, null);
    }

    [Fact]
    public async Task Moves_appointment_to_target_session_when_class_matches_and_seat_is_free()
    {
        var source = CreateSession(BoatClass.Quad4x);
        var target = CreateSession(BoatClass.Quad4x);
        var appointment = CreateAppointment(source);

        _appointmentRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        _sessionRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);

        var handler = CreateHandler();
        await handler.Handle(new MoveAppointmentToSessionCommand(appointment.Id, target.Id), CancellationToken.None);

        appointment.SessionId.Should().Be(target.Id);
        source.Appointments.Should().NotContain(appointment);
        target.Appointments.Should().Contain(appointment);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_move_to_a_session_of_a_different_boat_class()
    {
        var source = CreateSession(BoatClass.Quad4x);
        var target = CreateSession(BoatClass.Single1x);
        var appointment = CreateAppointment(source);

        _appointmentRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        _sessionRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);

        var handler = CreateHandler();
        var act = () => handler.Handle(new MoveAppointmentToSessionCommand(appointment.Id, target.Id), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("boat_class_mismatch");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_move_to_a_session_that_is_already_full()
    {
        var source = CreateSession(BoatClass.Single1x);
        var target = CreateSession(BoatClass.Single1x);
        CreateAppointment(target);
        var appointment = CreateAppointment(source);

        _appointmentRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        _sessionRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);

        var handler = CreateHandler();
        var act = () => handler.Handle(new MoveAppointmentToSessionCommand(appointment.Id, target.Id), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("session_full");
    }

    [Fact]
    public async Task Rejects_move_of_a_cancelled_appointment()
    {
        var source = CreateSession(BoatClass.Quad4x);
        var target = CreateSession(BoatClass.Quad4x);
        var appointment = CreateAppointment(source);
        appointment.SetStatus(AppointmentStatus.Cancelled);

        _appointmentRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        _sessionRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);

        var handler = CreateHandler();
        var act = () => handler.Handle(new MoveAppointmentToSessionCommand(appointment.Id, target.Id), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("appointment_cancelled");
    }
}
