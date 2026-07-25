using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Jwt;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Login;
using RowingClub.Identity.Application.Tokens;
using RowingClub.Identity.Application.TwoFactor;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.UnitTests.Identity.Application;

public sealed class LoginCommandHandler2faTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICredentialRepository _credentialRepository = Substitute.For<ICredentialRepository>();
    private readonly IUserSessionRepository _userSessionRepository = Substitute.For<IUserSessionRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
    private readonly IOpaqueTokenGenerator _opaqueTokenGenerator = Substitute.For<IOpaqueTokenGenerator>();
    private readonly IRefreshTokenHasher _refreshTokenHasher = Substitute.For<IRefreshTokenHasher>();

    private readonly IdentityOptions _options = new()
    {
        MaxFailedLoginAttempts = 5,
        LockoutDuration = TimeSpan.FromMinutes(15),
        RefreshTokenLifetime = TimeSpan.FromDays(30),
        PendingTwoFactorTokenLifetime = TimeSpan.FromMinutes(5),
    };

    private LoginCommandHandler CreateHandler()
    {
        var tokenPairIssuer = new TokenPairIssuer(
            _jwtTokenService, _opaqueTokenGenerator, _refreshTokenHasher,
            _refreshTokenRepository, Options.Create(_options));

        return new LoginCommandHandler(
            _userRepository, _credentialRepository, _userSessionRepository,
            _passwordHasher, tokenPairIssuer, Options.Create(_options),
            _auditLogger);
    }

    private static User CreateActiveUser()
    {
        var user = User.Register(EmailAddress.Create("test@example.com"));
        user.VerifyEmail();
        return user;
    }

    [Fact]
    public async Task Handle_returns_complete_result_when_2fa_is_disabled()
    {
        var user = CreateActiveUser();
        var credential = Credential.Create(user.Id, "hashed-pw");

        _userRepository.GetByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentialRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(credential);
        _passwordHasher.Verify("correct-pw", "hashed-pw").Returns(true);
        _jwtTokenService.IssueAccessToken(user.Id, user.Email.Value, Arg.Any<System.Collections.Generic.IReadOnlyCollection<System.Security.Claims.Claim>?>())
            .Returns(new IssuedAccessToken("at", DateTimeOffset.UtcNow.AddMinutes(15), "jti"));
        _opaqueTokenGenerator.Generate().Returns("raw-rt");
        _refreshTokenHasher.Hash("raw-rt").Returns("hashed-rt");

        var result = await CreateHandler().Handle(
            new LoginCommand("test@example.com", "correct-pw", "device"), CancellationToken.None);

        result.RequiresTwoFactor.Should().BeFalse();
        result.AccessToken.Should().Be("at");
        result.RefreshToken.Should().Be("raw-rt");
    }

    [Fact]
    public async Task Handle_bypasses_two_factor_when_user_has_totp_enabled()
    {
        var user = CreateActiveUser();
        user.EnableTwoFactor("Totp", TwoFactorService.GenerateTotpSecret());
        var credential = Credential.Create(user.Id, "hashed-pw");

        _userRepository.GetByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentialRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(credential);
        _passwordHasher.Verify("correct-pw", "hashed-pw").Returns(true);
        _jwtTokenService.IssueAccessToken(user.Id, user.Email.Value, Arg.Any<System.Collections.Generic.IReadOnlyCollection<System.Security.Claims.Claim>?>())
            .Returns(new IssuedAccessToken("at", DateTimeOffset.UtcNow.AddMinutes(15), "jti"));

        var result = await CreateHandler().Handle(
            new LoginCommand("test@example.com", "correct-pw", "device"), CancellationToken.None);

        result.RequiresTwoFactor.Should().BeFalse();
        result.AccessToken.Should().Be("at");
        result.RefreshToken.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_still_rejects_after_max_failed_attempts()
    {
        var user = CreateActiveUser();
        var credential = Credential.Create(user.Id, "hashed-pw");

        _userRepository.GetByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentialRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(credential);
        _passwordHasher.Verify("wrong-pw", "hashed-pw").Returns(false);

        for (int i = 0; i < 5; i++)
        {
            var act = () => CreateHandler().Handle(
                new LoginCommand("test@example.com", "wrong-pw", "device"), CancellationToken.None);
            await act.Should().ThrowAsync<AuthenticationFailedException>();
        }

        user.IsLockedOut().Should().BeTrue();
        user.FailedLoginAttemptCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_uses_generic_error_message_for_wrong_password()
    {
        var user = CreateActiveUser();
        var credential = Credential.Create(user.Id, "hashed-pw");

        _userRepository.GetByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentialRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(credential);
        _passwordHasher.Verify("wrong", "hashed-pw").Returns(false);

        var act = () => CreateHandler().Handle(
            new LoginCommand("test@example.com", "wrong", null), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthenticationFailedException>();
        ex.Which.Message.Should().Be("E-posta veya parola hatalı.");
    }

    [Fact]
    public async Task Handle_uses_same_generic_message_for_missing_user()
    {
        _userRepository.GetByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => CreateHandler().Handle(
            new LoginCommand("missing@example.com", "anything", null), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthenticationFailedException>();
        ex.Which.Message.Should().Be("E-posta veya parola hatalı.");
    }
}
