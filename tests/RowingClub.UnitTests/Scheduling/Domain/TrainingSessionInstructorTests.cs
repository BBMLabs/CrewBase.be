using FluentAssertions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Instructors;
using RowingClub.Scheduling.Domain.Sessions;

namespace RowingClub.UnitTests.Scheduling.Domain;

public sealed class TrainingSessionInstructorTests
{
    private static TrainingSession CreateSession(BoatClass boatClass) =>
        TrainingSession.Create(DateOnly.FromDateTime(DateTime.Today), new TimeOnly(9, 0), boatClass, 0, null, null);

    [Theory]
    [InlineData(BoatClass.Single1x)]
    [InlineData(BoatClass.Double2x)]
    public void Rejects_instructor_on_boats_rowed_without_one(BoatClass boatClass)
    {
        var session = CreateSession(boatClass);
        var instructor = Instructor.Create("Ali Vural", null, null);

        var act = () => session.AssignInstructor(instructor);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("instructor_not_applicable");
        session.InstructorId.Should().BeNull();
    }

    [Fact]
    public void Assigns_instructor_to_a_quad_boat()
    {
        var session = CreateSession(BoatClass.Quad4x);
        var instructor = Instructor.Create("Ali Vural", null, null);

        session.AssignInstructor(instructor);

        session.ActiveInstructor.Should().Be(instructor);
        session.ActiveInstructorId.Should().Be(instructor.Id);
    }

    [Fact]
    public void Hides_a_legacy_instructor_left_on_a_single_boat()
    {
        var session = TrainingSession.Create(
            DateOnly.FromDateTime(DateTime.Today), new TimeOnly(9, 0), BoatClass.Single1x, 0, null, Guid.NewGuid());

        session.ActiveInstructorId.Should().BeNull();
        session.ActiveInstructor.Should().BeNull();
    }
}
