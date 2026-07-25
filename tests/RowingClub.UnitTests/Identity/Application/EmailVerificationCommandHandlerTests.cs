using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application.EmailVerification;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.UnitTests.Identity.Application;

public sealed class VerifyEmailCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailVerificationTokenRepository _tokenRepository = Substitute.For<IEmailVerificationTokenRepository>();
    private readonly IRefreshTokenHasher _hasher = Substitute.For<IRefreshTokenHasher>();

    private VerifyEmailCommandHandler CreateHandler() =>
        new(_userRepository, _tokenRepository, _hasher);

    private static User CreateUnverifiedUser()
    {
        var user = User.Register(EmailAddress.Create("user@example.com"));
        return user;
    }

    [Fact]
    public async Task Handle_verifies_email_when_token_is_valid()
    {
        var user = CreateUnverifiedUser();
        var token = EmailVerificationToken.Issue(user.Id, "hashed-token", TimeSpan.FromHours(24));

        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _tokenRepository.GetByTokenHashAsync("hashed-token", Arg.Any<CancellationToken>()).Returns(token);
        _hasher.Hash("raw-token").Returns("hashed-token");

        await CreateHandler().Handle(
            new VerifyEmailCommand("user@example.com", "raw-token"), CancellationToken.None);

        user.EmailVerified.Should().BeTrue();
        token.UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_throws_when_token_is_expired()
    {
        var user = CreateUnverifiedUser();
        var token = EmailVerificationToken.Issue(user.Id, "hashed-token", TimeSpan.FromHours(-1));

        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _tokenRepository.GetByTokenHashAsync("hashed-token", Arg.Any<CancellationToken>()).Returns(token);
        _hasher.Hash("raw-token").Returns("hashed-token");

        var act = () => CreateHandler().Handle(
            new VerifyEmailCommand("user@example.com", "raw-token"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_throws_when_token_is_already_used()
    {
        var user = CreateUnverifiedUser();
        var token = EmailVerificationToken.Issue(user.Id, "hashed-token", TimeSpan.FromHours(24));
        token.MarkUsed();

        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _tokenRepository.GetByTokenHashAsync("hashed-token", Arg.Any<CancellationToken>()).Returns(token);
        _hasher.Hash("raw-token").Returns("hashed-token");

        var act = () => CreateHandler().Handle(
            new VerifyEmailCommand("user@example.com", "raw-token"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_does_nothing_when_email_already_verified()
    {
        var user = CreateUnverifiedUser();
        user.VerifyEmail();

        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        await CreateHandler().Handle(
            new VerifyEmailCommand("user@example.com", "anything"), CancellationToken.None);

        await _tokenRepository.DidNotReceive().GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
