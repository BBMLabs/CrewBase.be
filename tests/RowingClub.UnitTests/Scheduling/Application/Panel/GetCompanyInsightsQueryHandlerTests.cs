using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Panel;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;

namespace RowingClub.UnitTests.Scheduling.Application.Panel;

public sealed class GetCompanyInsightsQueryHandlerTests
{
    private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
    private readonly IBoatRepository _boatRepository = Substitute.For<IBoatRepository>();
    private readonly ITenantDatabase _tenantDatabase = Substitute.For<ITenantDatabase>();

    private GetCompanyInsightsQueryHandler CreateHandler() =>
        new(_appointmentRepository, _boatRepository, _tenantDatabase);

    [Fact]
    public async Task Throws_plan_feature_not_available_when_advanced_reports_not_available()
    {
        _tenantDatabase.HasAdvancedReports.Returns(false);

        var handler = CreateHandler();
        var act = () => handler.Handle(new GetCompanyInsightsQuery(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("plan_feature_not_available");
    }

    [Fact]
    public async Task Returns_company_insights_dto_when_advanced_reports_available()
    {
        _tenantDatabase.HasAdvancedReports.Returns(true);
        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Appointment>());
        _boatRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Boat>());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetCompanyInsightsQuery(), CancellationToken.None);

        result.TopBoats.Should().BeEmpty();
        result.TopMembers.Should().BeEmpty();
        result.BusiestWeekdays.Should().HaveCount(7);
    }
}
