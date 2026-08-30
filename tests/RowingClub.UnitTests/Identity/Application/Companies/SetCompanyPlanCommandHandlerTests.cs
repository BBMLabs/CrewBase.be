using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Companies.SetCompanyPlan;
using RowingClub.Identity.Domain.Companies;
using RowingClub.UnitTests.Identity.Application;

namespace RowingClub.UnitTests.Identity.Application.Companies;

public sealed class SetCompanyPlanCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();

    private SetCompanyPlanCommandHandler CreateHandler() => new(_companyRepository, _auditLogger);

    [Fact]
    public async Task Throws_company_not_found_when_company_does_not_exist()
    {
        var companyId = Guid.NewGuid();
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns((Company?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new SetCompanyPlanCommand(companyId, "Tayfa", null, null, null, null), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("company_not_found");
    }

    [Fact]
    public async Task Throws_invalid_plan_when_plan_name_cannot_be_parsed()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new SetCompanyPlanCommand(company.Id, "NotAPlan", null, null, null, null), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_plan");
    }

    [Fact]
    public async Task Throws_custom_limits_required_when_custom_plan_missing_limits()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new SetCompanyPlanCommand(company.Id, "Custom", null, null, null, null), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("custom_limits_required");
    }

    [Fact]
    public async Task Sets_fixed_plan_and_persists_when_valid()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        await handler.Handle(new SetCompanyPlanCommand(company.Id, "Amiral", null, null, null, null), CancellationToken.None);

        company.Plan.Should().Be(CompanyPlan.Amiral);
        _companyRepository.Received(1).Update(company);
        _auditLogger.Received(1).Log("COMPANY_PLAN_SET", company.Id.ToString(), Arg.Any<string>());
    }

    [Fact]
    public async Task Sets_custom_plan_with_limits_and_persists_when_valid()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        await handler.Handle(
            new SetCompanyPlanCommand(
                company.Id, "Custom", CustomMaxBranches: 10, CustomMaxMembers: 500, CustomMaxBoats: 40,
                CustomMaxInstructors: 20, CustomMaxManagers: 5, CustomMaxEmployees: 8, CustomCanExportData: true,
                CustomHasAdvancedReports: true, CustomHasAutomaticDuesReminders: false),
            CancellationToken.None);

        company.Plan.Should().Be(CompanyPlan.Custom);
        company.CustomMaxBranches.Should().Be(10);
        company.CustomMaxMembers.Should().Be(500);
        company.CustomMaxBoats.Should().Be(40);
        company.CustomMaxInstructors.Should().Be(20);
        company.PlanFeatures.MaxManagers.Should().Be(5);
        company.PlanFeatures.MaxEmployees.Should().Be(8);
        company.PlanFeatures.CanExportData.Should().BeTrue();
        company.PlanFeatures.HasAdvancedReports.Should().BeTrue();
        company.PlanFeatures.HasAutomaticDuesReminders.Should().BeFalse();
        _companyRepository.Received(1).Update(company);
    }
}
