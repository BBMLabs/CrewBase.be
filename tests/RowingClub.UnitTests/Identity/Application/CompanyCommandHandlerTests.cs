using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Companies.ApproveCompany;
using RowingClub.Identity.Application.Companies.GetPendingCompanies;
using RowingClub.Identity.Application.Companies.SuspendCompany;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.UnitTests.Identity.Application;

internal static class CompanyTestFactory
{
    public static Company Create(string name = "Rowing Club")
    {
        var subdomain = SubdomainSlug.FromCompanyName(name);
        return Company.Register(name, subdomain, SubdomainSlug.ToDatabaseName(subdomain), null, null, null, null);
    }
}

public sealed class ApproveCompanyCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private ApproveCompanyCommandHandler CreateHandler() =>
        new(_companyRepository, _userRepository);

    [Fact]
    public async Task Companies_are_active_immediately_after_registration()
    {
        var company = CompanyTestFactory.Create();

        company.Status.Should().Be(CompanyStatus.Active);
        company.ApprovedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_keeps_already_active_company_active()
    {
        var company = CompanyTestFactory.Create();
        var adminUser = User.Register(EmailAddress.Create("admin@example.com"));

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _userRepository.GetByIdAsync(adminUser.Id, Arg.Any<CancellationToken>()).Returns(adminUser);

        await CreateHandler().Handle(
            new ApproveCompanyCommand(company.Id, adminUser.Id), CancellationToken.None);

        company.Status.Should().Be(CompanyStatus.Active);
        _companyRepository.Received(1).Update(company);
    }

    [Fact]
    public async Task Handle_throws_when_company_not_found()
    {
        var companyId = Guid.NewGuid();
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        var act = () => CreateHandler().Handle(
            new ApproveCompanyCommand(companyId, Guid.NewGuid()), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.ErrorCode.Should().Be("company_not_found");
    }

    [Fact]
    public async Task Handle_reactivates_suspended_company()
    {
        var company = CompanyTestFactory.Create();
        company.Suspend();
        var adminUser = User.Register(EmailAddress.Create("admin@example.com"));

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _userRepository.GetByIdAsync(adminUser.Id, Arg.Any<CancellationToken>()).Returns(adminUser);

        await CreateHandler().Handle(
            new ApproveCompanyCommand(company.Id, adminUser.Id), CancellationToken.None);

        company.Status.Should().Be(CompanyStatus.Active);
        company.ApprovedByUserId.Should().Be(adminUser.Id);
    }
}

public sealed class SuspendCompanyCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();

    private SuspendCompanyCommandHandler CreateHandler() =>
        new(_companyRepository);

    [Fact]
    public async Task Handle_suspends_active_company()
    {
        var company = CompanyTestFactory.Create();

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        await CreateHandler().Handle(
            new SuspendCompanyCommand(company.Id), CancellationToken.None);

        company.Status.Should().Be(CompanyStatus.Suspended);
        _companyRepository.Received(1).Update(company);
    }

    [Fact]
    public async Task Handle_throws_when_company_not_found()
    {
        _companyRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        var act = () => CreateHandler().Handle(
            new SuspendCompanyCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public sealed class GetPendingCompaniesQueryHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();

    private GetPendingCompaniesQueryHandler CreateHandler() =>
        new(_companyRepository);

    [Fact]
    public async Task Handle_returns_companies_reported_by_repository()
    {
        var company = CompanyTestFactory.Create("Club A");

        _companyRepository.GetByStatusAsync(CompanyStatus.PendingApproval, Arg.Any<CancellationToken>())
            .Returns(new List<Company> { company });

        var result = await CreateHandler().Handle(
            new GetPendingCompaniesQuery(), CancellationToken.None);

        result.Should().ContainSingle(c => c.Name == "Club A");
    }
}
