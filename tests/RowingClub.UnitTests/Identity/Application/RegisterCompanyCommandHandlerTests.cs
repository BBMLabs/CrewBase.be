using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.BuildingBlocks.Security.Tokens;
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
    private readonly ITenantDatabaseProvisioner _provisioner = Substitute.For<ITenantDatabaseProvisioner>();
    private readonly IOpaqueTokenGenerator _tokenGenerator = Substitute.For<IOpaqueTokenGenerator>();

    private RegisterCompanyCommandHandler CreateHandler() =>
        new(_companyRepository, _userRepository, _credentialRepository, _passwordHasher,
            _provisioner, _tokenGenerator);

    public RegisterCompanyCommandHandlerTests()
    {
        _tokenGenerator.Generate().Returns("random-opaque-token");
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");
    }

    [Fact]
    public async Task Handle_creates_company_and_company_admin()
    {
        _companyRepository.ExistsByNameAsync("Rowing Club", Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.ExistsByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>()).Returns(false);

        var response = await CreateHandler().Handle(
            new RegisterCompanyCommand("Rowing Club", "admin@example.com", "1234567890",
                "+905551234567", "contact@example.com", "İstanbul"), CancellationToken.None);

        response.CompanyName.Should().Be("Rowing Club");
        response.AdminEmail.Should().Be("admin@example.com");
        response.Subdomain.Should().Be("rowing-club");
        response.SiteUrl.Should().Be("https://rowing-club.faturebase.com");
        await _provisioner.Received(1).ProvisionAsync("tenant_rowing_club", Arg.Any<CancellationToken>());
        _companyRepository.Received(1).Add(Arg.Is<Company>(c =>
            c!.Name == "Rowing Club" && c.Status == CompanyStatus.Active && c.Subdomain == "rowing-club"
            && c.TaxNumber == "1234567890"));
        _userRepository.Received(1).Add(Arg.Is<User>(u =>
            u!.Email.Value == "admin@example.com" && u.Role == UserRole.CompanyAdmin));
        // Parola kayıtta alınmaz: kullanıcının hiç bilmediği rastgele bir hash ile Credential açılır.
        _credentialRepository.Received(1).Add(Arg.Any<Credential>());
        // Aktivasyon e-postası burada gönderilmez: e-posta doğrulanana kadar bekler
        // (bkz. VerifyEmailCommandHandlerTests).
    }

    [Fact]
    public async Task Handle_throws_when_company_name_is_taken()
    {
        _companyRepository.ExistsByNameAsync("Rowing Club", Arg.Any<CancellationToken>()).Returns(true);

        var act = () => CreateHandler().Handle(
            new RegisterCompanyCommand("Rowing Club", "admin@example.com", "1234567890",
                "+905551234567", "contact@example.com", "İstanbul"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which!.ErrorCode.Should().Be("company_name_taken");
    }

    [Fact]
    public async Task Handle_throws_when_email_is_already_registered()
    {
        _companyRepository.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.ExistsByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>()).Returns(true);

        var act = () => CreateHandler().Handle(
            new RegisterCompanyCommand("Rowing Club", "admin@example.com", "1234567890",
                "+905551234567", "contact@example.com", "İstanbul"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which!.ErrorCode.Should().Be("email_already_registered");
    }
}
