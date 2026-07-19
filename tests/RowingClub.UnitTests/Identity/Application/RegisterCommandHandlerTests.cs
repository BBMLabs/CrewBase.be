using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Identity.Application.Register;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.UnitTests.Identity.Application;

public sealed class RegisterCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICredentialRepository _credentialRepository = Substitute.For<ICredentialRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    private RegisterCommandHandler CreateHandler() =>
        new(_userRepository, _credentialRepository, _passwordHasher);

    [Fact]
    public async Task Handle_creates_user_and_credential_when_email_is_not_taken()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _passwordHasher.Hash("SecurePass123").Returns("hashed-password");

        var response = await CreateHandler().Handle(
            new RegisterCommand("test@example.com", "SecurePass123"), CancellationToken.None);

        response.Email.Should().Be("test@example.com");
        _userRepository.Received(1).Add(Arg.Is<User>(u => u!.Email.Value == "test@example.com"));
        _credentialRepository.Received(1).Add(Arg.Is<Credential>(c => c!.PasswordHash == "hashed-password"));
    }

    [Fact]
    public async Task Handle_throws_when_email_is_already_registered()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var act = () => CreateHandler().Handle(
            new RegisterCommand("test@example.com", "SecurePass123"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<DomainException>();
        exception.Which.ErrorCode.Should().Be("email_already_registered");

        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }
}
