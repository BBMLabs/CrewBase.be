using FluentAssertions;
using NSubstitute;
using RowingClub.Scheduling.Application.Panel;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Sessions;

namespace RowingClub.UnitTests.Scheduling.Application.Panel;

public sealed class SetAppointmentStatusCommandHandlerTests
{
    private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
    private readonly ICustomerPackageRepository _customerPackageRepository = Substitute.For<ICustomerPackageRepository>();
    private readonly IMemberLogRepository _memberLogRepository = Substitute.For<IMemberLogRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private SetAppointmentStatusCommandHandler CreateHandler() =>
        new(_appointmentRepository, _customerPackageRepository, _memberLogRepository, _unitOfWork);

    private (Appointment Appointment, CustomerPackage Package) CreatePackageBooking()
    {
        var customer = Customer.Create("Ada Lovelace", "+905551112233", "ada@example.com");
        var package = CustomerPackage.Assign(
            customer.Id, LessonPackage.Create("8 Ders", null, 8, 100m, null), CustomerPackageSource.Assigned);
        package.Deduct();
        var session = TrainingSession.Create(
            DateOnly.FromDateTime(DateTime.Today), new TimeOnly(9, 0), BoatClass.Quad4x, 0, null, null);
        var appointment = Appointment.Book(customer, session, null, null, package.Id);

        _appointmentRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        _customerPackageRepository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);
        return (appointment, package);
    }

    [Fact]
    public async Task Cancelling_with_refund_returns_the_lesson_to_the_package()
    {
        var (appointment, package) = CreatePackageBooking();

        await CreateHandler().Handle(
            new SetAppointmentStatusCommand(appointment.Id, "Cancelled", RefundPackage: true), CancellationToken.None);

        package.RemainingSessions.Should().Be(8);
        appointment.UsedPackage.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancelling_without_refund_keeps_the_lesson_deducted()
    {
        var (appointment, package) = CreatePackageBooking();

        await CreateHandler().Handle(
            new SetAppointmentStatusCommand(appointment.Id, "Cancelled", RefundPackage: false), CancellationToken.None);

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        package.RemainingSessions.Should().Be(7);
        appointment.UsedPackage.Should().BeFalse();
    }

    [Fact]
    public async Task Reopening_a_cancellation_that_kept_the_deduction_does_not_deduct_again()
    {
        var (appointment, package) = CreatePackageBooking();
        var handler = CreateHandler();

        await handler.Handle(
            new SetAppointmentStatusCommand(appointment.Id, "Cancelled", RefundPackage: false), CancellationToken.None);
        await handler.Handle(new SetAppointmentStatusCommand(appointment.Id, "Pending"), CancellationToken.None);

        package.RemainingSessions.Should().Be(7);
    }
}
