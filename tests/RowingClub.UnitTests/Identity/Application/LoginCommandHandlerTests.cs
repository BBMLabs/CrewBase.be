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

public sealed class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICredentialRepository _credentialRepository = Substitute.For<ICredentialRepository>();
    private readonly IUserSessionRepository _userSessionRepository = Substitute.For<IUserSessionRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IOpaqueTokenGenerator _opaqueTokenGenerator = Substitute.For<IOpaqueTokenGenerator>();
    private readonly IRefreshTokenHasher _refreshTokenHasher = Substitute.For<IRefreshTokenHasher>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
    private readonly IPendingTwoFactorTokenRepository _pendingTwoFactorTokenRepository = Substitute.For<IPendingTwoFactorTokenRepository>();

    private readonly IdentityOptions _identityOptions = new()
    {
        MaxFailedLoginAttempts = 5,
        LockoutDuration = TimeSpan.FromMinutes(15),
        RefreshTokenLifetime = TimeSpan.FromDays(30),
        PendingTwoFactorTokenLifetime = TimeSpan.FromMinutes(5),
    };

    private LoginCommandHandler CreateHandler()
    {
        var tokenPairIssuer = new TokenPairIssuer(
            _jwtTokenService,
            _opaqueTokenGenerator,
            _refreshTokenHasher,
            _refreshTokenRepository,
            Options.Create(_identityOptions));

        return new LoginCommandHandler(
            _userRepository,
            _credentialRepository,
            _userSessionRepository,
            _passwordHasher,
            tokenPairIssuer,
            Options.Create(_identityOptions),
            _auditLogger,
            _pendingTwoFactorTokenRepository,
            _refreshTokenHasher,
            _opaqueTokenGenerator);
    }

    private static User CreateActiveUser() => User.Register(EmailAddress.Create("test@example.com"));

    [Fact]
    public async Task Handle_returns_token_pair_on_correct_credentials()
    {
        var user = CreateActiveUser();
        var credential = Credential.Create(user.Id, "hashed-password");

        _userRepository.GetByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentialRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(credential);
        _passwordHasher.Verify("correct-password", "hashed-password").Returns(true);
        _jwtTokenService.IssueAccessToken(user.Id, user.Email.Value, Arg.Any<System.Collections.Generic.IReadOnlyCollection<System.Security.Claims.Claim>?>())
            .Returns(new IssuedAccessToken("access-token", DateTimeOffset.UtcNow.AddMinutes(15), "jti"));
        _opaqueTokenGenerator.Generate().Returns("raw-refresh-token");
        _refreshTokenHasher.Hash("raw-refresh-token").Returns("hashed-refresh-token");

        var result = await CreateHandler().Handle(
            new LoginCommand("test@example.com", "correct-password", "unit-test-device"), CancellationToken.None);

        result.RequiresTwoFactor.Should().BeFalse();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh-token");
        _userSessionRepository.Received(1).Add(Arg.Any<UserSession>());
        _refreshTokenRepository.Received(1).Add(Arg.Any<RefreshToken>());
    }

    [Fact]
    public async Task Handle_throws_generic_error_on_wrong_password_without_revealing_which_field_was_wrong()
    {
        var user = CreateActiveUser();
        var credential = Credential.Create(user.Id, "hashed-password");

        _userRepository.GetByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentialRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(credential);
        _passwordHasher.Verify("wrong-password", "hashed-password").Returns(false);

        var act = () => CreateHandler().Handle(
            new LoginCommand("test@example.com", "wrong-password", null), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        user.FailedLoginAttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_throws_when_user_does_not_exist()
    {
        _userRepository.GetByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => CreateHandler().Handle(
            new LoginCommand("missing@example.com", "whatever", null), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_returns_two_factor_required_when_user_has_2fa_enabled()
    {
        var user = CreateActiveUser();
        user.EnableTwoFactor("Totp", TwoFactorService.GenerateTotpSecret());
        var credential = Credential.Create(user.Id, "hashed-password");

        _userRepository.GetByEmailAsync(Arg.Any<EmailAddress>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentialRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(credential);
        _passwordHasher.Verify("correct-password", "hashed-password").Returns(true);
        _opaqueTokenGenerator.Generate().Returns("pending-token-raw");
        _refreshTokenHasher.Hash("pending-token-raw").Returns("pending-token-hash");

        var result = await CreateHandler().Handle(
            new LoginCommand("test@example.com", "correct-password", "unit-test-device"), CancellationToken.None);

        result.RequiresTwoFactor.Should().BeTrue();
        result.PendingTwoFactorToken.Should().Be("pending-token-raw");
        result.AccessToken.Should().BeNull();
        _pendingTwoFactorTokenRepository.Received(1).Add(Arg.Any<PendingTwoFactorToken>());
    }
}
