using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Application.PasswordReset;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.UnitTests.Identity.Application;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordResetTokenRepository _tokenRepository = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IOpaqueTokenGenerator _tokenGenerator = Substitute.For<IOpaqueTokenGenerator>();
    private readonly IRefreshTokenHasher _hasher = Substitute.For<IRefreshTokenHasher>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();

    private ForgotPasswordCommandHandler CreateHandler() =>
        new(_userRepository, _tokenRepository, _tokenGenerator, _hasher, _emailSender);

    [Fact]
    public async Task Handle_creates_token_when_user_exists()
    {
        var email = EmailAddress.Create("user@example.com");
        var user = User.Register(email);

        _userRepository.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(user);
        _tokenGenerator.Generate().Returns("raw-token");
        _hasher.Hash("raw-token").Returns("hashed-token");

        await CreateHandler().Handle(
            new ForgotPasswordCommand("user@example.com"), CancellationToken.None);

        _tokenRepository.Received(1).Add(Arg.Is<PasswordResetToken>(t =>
            t!.UserId == user.Id && t.TokenHash == "hashed-token"));
    }

    [Fact]
    public async Task Handle_does_not_reveal_whether_email_exists()
    {
        _userRepository.GetByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await CreateHandler().Handle(
            new ForgotPasswordCommand("missing@example.com"), CancellationToken.None);

        _tokenRepository.DidNotReceive().Add(Arg.Any<PasswordResetToken>());
    }
}

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordResetTokenRepository _tokenRepository = Substitute.For<IPasswordResetTokenRepository>();
    private readonly ICredentialRepository _credentialRepository = Substitute.For<ICredentialRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IRefreshTokenHasher _hasher = Substitute.For<IRefreshTokenHasher>();

    private ResetPasswordCommandHandler CreateHandler() =>
        new(_userRepository, _credentialRepository, _tokenRepository, _hasher, _passwordHasher);

    private static PasswordResetToken CreateValidToken(Guid userId) =>
        PasswordResetToken.Issue(userId, "hashed-token", TimeSpan.FromHours(1));

    [Fact]
    public async Task Handle_resets_password_when_token_is_valid()
    {
        var email = EmailAddress.Create("user@example.com");
        var user = User.Register(email);
        var credential = Credential.Create(user.Id, "old-hash");
        var token = CreateValidToken(user.Id);

        _userRepository.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(user);
        _tokenRepository.GetByTokenHashAsync("hashed-token", Arg.Any<CancellationToken>()).Returns(token);
        _credentialRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(credential);
        _hasher.Hash("raw-token").Returns("hashed-token");
        _passwordHasher.Hash("new-password").Returns("new-hash");

        await CreateHandler().Handle(
            new ResetPasswordCommand("user@example.com", "raw-token", "new-password"), CancellationToken.None);

        credential.PasswordHash.Should().Be("new-hash");
        token!.UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_throws_when_token_is_expired()
    {
        var email = EmailAddress.Create("user@example.com");
        var user = User.Register(email);
        var token = PasswordResetToken.Issue(user.Id, "hashed-token", TimeSpan.FromHours(-1));

        _userRepository.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(user);
        _tokenRepository.GetByTokenHashAsync("hashed-token", Arg.Any<CancellationToken>()).Returns(token);
        _hasher.Hash("raw-token").Returns("hashed-token");

        var act = () => CreateHandler().Handle(
            new ResetPasswordCommand("user@example.com", "raw-token", "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_throws_when_token_is_already_used()
    {
        var email = EmailAddress.Create("user@example.com");
        var user = User.Register(email);
        var token = CreateValidToken(user.Id);
        token.MarkUsed();

        _userRepository.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(user);
        _tokenRepository.GetByTokenHashAsync("hashed-token", Arg.Any<CancellationToken>()).Returns(token);
        _hasher.Hash("raw-token").Returns("hashed-token");

        var act = () => CreateHandler().Handle(
            new ResetPasswordCommand("user@example.com", "raw-token", "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_throws_for_nonexistent_user()
    {
        _userRepository.GetByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed-token");

        var act = () => CreateHandler().Handle(
            new ResetPasswordCommand("missing@example.com", "token", "pwd"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
