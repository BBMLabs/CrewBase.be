using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Identity.Application.Companies.RegisterCompany;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.UnitTests.Identity.Application;

public sealed class RegisterCompanyCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICredentialRepository _credentialRepository = Substitute.For<ICredentialRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    private RegisterCompanyCommandHandler CreateHandler() =>
        new(_companyRepository, _userRepository, _credentialRepository, _passwordHasher);

    [Fact]
    public async Task Handle_creates_company_and_company_admin()
    {
        _companyRepository.ExistsByNameAsync("Rowing Club", Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.ExistsByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash("SecurePass123").Returns("hashed-password");

        var response = await CreateHandler().Handle(
            new RegisterCompanyCommand("Rowing Club", "admin@example.com", "SecurePass123",
                null, null, null), CancellationToken.None);

        response.CompanyName.Should().Be("Rowing Club");
        response.AdminEmail.Should().Be("admin@example.com");
        _companyRepository.Received(1).Add(Arg.Is<Company>(c => c!.Name == "Rowing Club"));
        _userRepository.Received(1).Add(Arg.Is<User>(u =>
            u!.Email.Value == "admin@example.com" && u.Role == UserRole.CompanyAdmin));
        _credentialRepository.Received(1).Add(Arg.Is<Credential>(c =>
            c!.PasswordHash == "hashed-password"));
    }

    [Fact]
    public async Task Handle_throws_when_company_name_is_taken()
    {
        _companyRepository.ExistsByNameAsync("Rowing Club", Arg.Any<CancellationToken>()).Returns(true);

        var act = () => CreateHandler().Handle(
            new RegisterCompanyCommand("Rowing Club", "admin@example.com", "SecurePass123",
                null, null, null), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which!.ErrorCode.Should().Be("company_name_taken");
    }

    [Fact]
    public async Task Handle_throws_when_email_is_already_registered()
    {
        _companyRepository.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.ExistsByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>()).Returns(true);

        var act = () => CreateHandler().Handle(
            new RegisterCompanyCommand("Rowing Club", "admin@example.com", "SecurePass123",
                null, null, null), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which!.ErrorCode.Should().Be("email_already_registered");
    }
}
