using FluentAssertions;
using NSubstitute;
using RowingClub.Scheduling.Application.Panel;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Sessions;

namespace RowingClub.UnitTests.Scheduling.Application.Panel;

public sealed class GetSessionByIdQueryHandlerTests
{
    private readonly ITrainingSessionRepository _sessionRepository = Substitute.For<ITrainingSessionRepository>();

    private GetSessionByIdQueryHandler CreateHandler() => new(_sessionRepository);

    [Fact]
    public async Task Returns_session_with_members_for_a_date_other_than_today()
    {
        // Regresyon: /company/sessions/{id}/appointments seansı yalnızca bugünün seansları arasında
        // arıyordu; başka günün seansı için session_not_found dönüyordu.
        var date = DateOnly.FromDateTime(DateTime.Today).AddDays(10);
        var session = TrainingSession.Create(date, new TimeOnly(8, 0), BoatClass.Quad4x, 1, null, null);
        var customer = Customer.Create("Ada Lovelace", "+905551112233", "ada@example.com");
        Appointment.Book(customer, session, null, null, null);
        _sessionRepository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateHandler().Handle(new GetSessionByIdQuery(session.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Date.Should().Be(date);
        result.Members.Should().ContainSingle(m => m.FullName == "Ada Lovelace");
    }

    [Fact]
    public async Task Returns_null_when_session_does_not_exist()
    {
        _sessionRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TrainingSession?)null);

        var result = await CreateHandler().Handle(new GetSessionByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}
