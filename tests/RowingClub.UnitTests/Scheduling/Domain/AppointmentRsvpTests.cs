using FluentAssertions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Sessions;

namespace RowingClub.UnitTests.Scheduling.Domain;

public sealed class AppointmentRsvpTests
{
    private static readonly DateTimeOffset BookedAt = new(2026, 9, 24, 12, 40, 0, TimeSpan.Zero);

    private static Appointment CreateAppointmentWithRsvp()
    {
        var customer = Customer.Create("Ada Lovelace", "+905551112233", "ada@example.com");
        var session = TrainingSession.Create(
            new DateOnly(2026, 9, 30), new TimeOnly(9, 0), BoatClass.Quad4x, 0, null, null);
        var appointment = Appointment.Book(customer, session, null, null, null);
        appointment.OpenRsvp("hash", BookedAt);
        return appointment;
    }

    [Fact]
    public void Booking_opens_a_one_hour_window_with_no_choice_and_pending_status()
    {
        var appointment = CreateAppointmentWithRsvp();

        appointment.Status.Should().Be(AppointmentStatus.Pending);
        appointment.RsvpChoice.Should().Be(RsvpChoice.None);
        appointment.RsvpDeadlineUtc.Should().Be(BookedAt.AddHours(1));
        appointment.IsRsvpOpen(BookedAt.AddMinutes(59)).Should().BeTrue();
    }

    [Fact]
    public void Choice_can_be_changed_repeatedly_within_the_window_without_touching_status()
    {
        var appointment = CreateAppointmentWithRsvp();

        appointment.ChooseRsvp(RsvpChoice.NotAttending, BookedAt.AddMinutes(10));
        appointment.ChooseRsvp(RsvpChoice.Attending, BookedAt.AddMinutes(20));
        appointment.ChooseRsvp(RsvpChoice.NotAttending, BookedAt.AddMinutes(59));

        appointment.RsvpChoice.Should().Be(RsvpChoice.NotAttending);
        appointment.Status.Should().Be(AppointmentStatus.Pending);
    }

    [Fact]
    public void Choice_is_rejected_after_the_deadline()
    {
        var appointment = CreateAppointmentWithRsvp();

        var act = () => appointment.ChooseRsvp(RsvpChoice.Attending, BookedAt.AddHours(1));

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("rsvp_closed");
    }

    [Fact]
    public void Choice_is_rejected_once_staff_changed_the_status()
    {
        var appointment = CreateAppointmentWithRsvp();
        appointment.SetStatus(AppointmentStatus.Confirmed);

        var act = () => appointment.ChooseRsvp(RsvpChoice.NotAttending, BookedAt.AddMinutes(5));

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("rsvp_closed");
    }

    [Fact]
    public void None_is_not_a_valid_choice()
    {
        var appointment = CreateAppointmentWithRsvp();

        var act = () => appointment.ChooseRsvp(RsvpChoice.None, BookedAt.AddMinutes(5));

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("rsvp_invalid_choice");
    }

    [Fact]
    public void Resolution_before_the_deadline_does_nothing()
    {
        var appointment = CreateAppointmentWithRsvp();

        appointment.ResolveRsvp(BookedAt.AddMinutes(30)).Should().BeNull();

        appointment.Status.Should().Be(AppointmentStatus.Pending);
        appointment.RsvpResolvedAtUtc.Should().BeNull();
    }

    [Fact]
    public void No_answer_confirms_the_appointment()
    {
        var appointment = CreateAppointmentWithRsvp();

        appointment.ResolveRsvp(BookedAt.AddHours(1)).Should().Be(AppointmentStatus.Confirmed);

        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
        appointment.RsvpResolvedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Attending_confirms_the_appointment()
    {
        var appointment = CreateAppointmentWithRsvp();
        appointment.ChooseRsvp(RsvpChoice.Attending, BookedAt.AddMinutes(5));

        appointment.ResolveRsvp(BookedAt.AddHours(2)).Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public void Not_attending_cancels_the_appointment()
    {
        var appointment = CreateAppointmentWithRsvp();
        appointment.ChooseRsvp(RsvpChoice.NotAttending, BookedAt.AddMinutes(5));

        appointment.ResolveRsvp(BookedAt.AddHours(2)).Should().Be(AppointmentStatus.Cancelled);

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public void Staff_decision_before_the_deadline_is_never_overridden()
    {
        var appointment = CreateAppointmentWithRsvp();
        appointment.ChooseRsvp(RsvpChoice.NotAttending, BookedAt.AddMinutes(5));
        appointment.SetStatus(AppointmentStatus.Confirmed);

        appointment.IsRsvpDue(BookedAt.AddHours(2)).Should().BeFalse();
        appointment.ResolveRsvp(BookedAt.AddHours(2)).Should().BeNull();

        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public void Resolution_is_idempotent()
    {
        var appointment = CreateAppointmentWithRsvp();
        appointment.ChooseRsvp(RsvpChoice.NotAttending, BookedAt.AddMinutes(5));

        appointment.ResolveRsvp(BookedAt.AddHours(2));
        var resolvedAt = appointment.RsvpResolvedAtUtc;

        appointment.ResolveRsvp(BookedAt.AddHours(3)).Should().BeNull();
        appointment.RsvpResolvedAtUtc.Should().Be(resolvedAt);
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public void Appointments_without_rsvp_are_never_due()
    {
        var customer = Customer.Create("Grace Hopper", "+905551112244", null);
        var session = TrainingSession.Create(
            new DateOnly(2026, 9, 30), new TimeOnly(9, 0), BoatClass.Quad4x, 0, null, null);
        var appointment = Appointment.Book(customer, session, null, null, null);

        appointment.HasRsvp.Should().BeFalse();
        appointment.RsvpChoice.Should().BeNull();
        appointment.ResolveRsvp(DateTimeOffset.UtcNow).Should().BeNull();
        appointment.Status.Should().Be(AppointmentStatus.Pending);
    }
}
