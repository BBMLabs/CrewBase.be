using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.CompanyUsers;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;
using RowingClub.UnitTests.Identity.Application;

namespace RowingClub.UnitTests.Identity.Application.CompanyUsers;

public sealed class CreateCompanyUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICredentialRepository _credentialRepository = Substitute.For<ICredentialRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();

    private CreateCompanyUserCommandHandler CreateHandler() =>
        new(_userRepository, _credentialRepository, _passwordHasher, _auditLogger, _companyRepository);

    private User SetUpCaller(Company company)
    {
        var caller = User.RegisterCompanyAdmin(EmailAddress.Create("admin@example.com"), company.Id);
        _userRepository.GetByIdAsync(caller.Id, Arg.Any<CancellationToken>()).Returns(caller);
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        return caller;
    }

    [Fact]
    public async Task Throws_plan_feature_not_available_when_assigning_employee_role_on_mico_plan()
    {
        var company = CompanyTestFactory.Create();
        var caller = SetUpCaller(company);
        _userRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(new List<User> { caller });

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new CreateCompanyUserCommand(caller.Id, "employee@example.com", "Passw0rd!", "Employee"),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("plan_feature_not_available");
    }

    [Fact]
    public async Task Throws_plan_limit_exceeded_when_company_user_count_at_plan_limit()
    {
        var company = CompanyTestFactory.Create();
        var caller = SetUpCaller(company);
        _userRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(new List<User> { caller });

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new CreateCompanyUserCommand(caller.Id, "another@example.com", "Passw0rd!", "CompanyAdmin"),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("plan_limit_exceeded");
    }

    [Fact]
    public async Task Creates_company_admin_user_on_mico_when_under_user_limit()
    {
        var company = CompanyTestFactory.Create();
        var caller = SetUpCaller(company);
        _userRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(new List<User>());
        _userRepository.ExistsByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");

        var handler = CreateHandler();
        var result = await handler.Handle(
            new CreateCompanyUserCommand(caller.Id, "newadmin@example.com", "Passw0rd!", "CompanyAdmin"),
            CancellationToken.None);

        result.Email.Should().Be("newadmin@example.com");
        result.Role.Should().Be("CompanyAdmin");
        _userRepository.Received(1).Add(Arg.Any<User>());
        _credentialRepository.Received(1).Add(Arg.Any<Credential>());
        _auditLogger.Received(1).Log("COMPANY_USER_CREATED", Arg.Any<string>(), Arg.Any<string>());
    }
}

public sealed class ChangeCompanyUserRoleCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();

    private ChangeCompanyUserRoleCommandHandler CreateHandler() =>
        new(_userRepository, _auditLogger, _companyRepository);

    private (User Caller, User Target) SetUp(Company company)
    {
        var caller = User.RegisterCompanyAdmin(EmailAddress.Create("admin@example.com"), company.Id);
        var target = User.RegisterCompanyAdmin(EmailAddress.Create("target@example.com"), company.Id);
        _userRepository.GetByIdAsync(caller.Id, Arg.Any<CancellationToken>()).Returns(caller);
        _userRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        return (caller, target);
    }

    [Fact]
    public async Task Throws_plan_feature_not_available_when_changing_role_to_employee_on_mico_plan()
    {
        var company = CompanyTestFactory.Create();
        var (caller, target) = SetUp(company);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new ChangeCompanyUserRoleCommand(caller.Id, target.Id, "Employee"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("plan_feature_not_available");
    }

    [Fact]
    public async Task Changes_role_to_employee_when_plan_is_kaptan()
    {
        var company = CompanyTestFactory.Create();
        company.SetPlan(CompanyPlan.Kaptan, null, null, null);
        var (caller, target) = SetUp(company);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ChangeCompanyUserRoleCommand(caller.Id, target.Id, "Employee"), CancellationToken.None);

        result.Role.Should().Be("Employee");
        target.Role.Should().Be(UserRole.Employee);
        _auditLogger.Received(1).Log("COMPANY_USER_ROLE_CHANGED", target.Id.ToString(), Arg.Any<string>());
    }
}
