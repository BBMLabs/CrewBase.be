using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Jwt;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Application.Refresh;
using RowingClub.Identity.Application.Tokens;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.UnitTests.Identity.Application;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserSessionRepository _userSessionRepository = Substitute.For<IUserSessionRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenHasher _refreshTokenHasher = Substitute.For<IRefreshTokenHasher>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IOpaqueTokenGenerator _opaqueTokenGenerator = Substitute.For<IOpaqueTokenGenerator>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

    private readonly IdentityOptions _options = new()
    {
        RefreshTokenLifetime = TimeSpan.FromDays(30),
    };

    private RefreshTokenCommandHandler CreateHandler()
    {
        var tokenPairIssuer = new TokenPairIssuer(
            _jwtTokenService, _opaqueTokenGenerator, _refreshTokenHasher,
            _refreshTokenRepository, Options.Create(_options));

        return new RefreshTokenCommandHandler(
            _refreshTokenRepository, _userSessionRepository, _userRepository,
            _refreshTokenHasher, tokenPairIssuer, _auditLogger, _emailSender,
            _passwordResetTokenRepository, _opaqueTokenGenerator, _configuration);
    }

    private static User CreateActiveUser()
    {
        var email = EmailAddress.Create("user@example.com");
        var user = User.Register(email);
        user.VerifyEmail();
        return user;
    }

    private static RefreshToken IssueToken(Guid userId, Guid familyId, string hash) =>
        RefreshToken.IssueInFamily(userId, familyId, hash, TimeSpan.FromDays(30));

    [Fact]
    public async Task Handle_rotates_token_and_touches_session_when_valid()
    {
        var user = CreateActiveUser();
        var familyId = Guid.NewGuid();
        var oldHash = "old-hash";
        var newHash = "new-hash";
        var token = IssueToken(user.Id, familyId, oldHash);
        var session = UserSession.Start(user.Id, familyId, "device");

        _refreshTokenRepository.GetByTokenHashAsync(oldHash, Arg.Any<CancellationToken>()).Returns(token);
        _userSessionRepository.GetByRefreshTokenFamilyIdAsync(familyId, Arg.Any<CancellationToken>()).Returns(session);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _refreshTokenHasher.Hash("raw-new-refresh").Returns(newHash);
        _refreshTokenHasher.Hash("raw-refresh").Returns(oldHash);
        _opaqueTokenGenerator.Generate().Returns("raw-new-refresh");
        _jwtTokenService.IssueAccessToken(user.Id, user.Email.Value, Arg.Any<System.Collections.Generic.IReadOnlyCollection<System.Security.Claims.Claim>?>())
            .Returns(new IssuedAccessToken("new-access-token", DateTimeOffset.UtcNow.AddMinutes(15), "jti"));

        var response = await CreateHandler().Handle(
            new RefreshTokenCommand("raw-refresh"), CancellationToken.None);

        response.AccessToken.Should().Be("new-access-token");
        token.ReplacedByTokenId.Should().NotBeNull();
        session.LastSeenAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_throws_generic_error_when_token_not_found()
    {
        _refreshTokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var act = () => CreateHandler().Handle(
            new RefreshTokenCommand("unknown"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_throws_generic_error_when_token_expired()
    {
        var token = RefreshToken.IssueNewFamily(Guid.NewGuid(), "hash", TimeSpan.FromDays(-1));
        _refreshTokenHasher.Hash("hash").Returns("hash");
        _refreshTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(token);

        var act = () => CreateHandler().Handle(
            new RefreshTokenCommand("hash"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_detects_theft_and_revokes_all_sessions_when_revoked_token_is_reused()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.IssueNewFamily(userId, "hash", TimeSpan.FromDays(30));
        var familyId = token.FamilyId;
        token.Revoke();

        var sibling = IssueToken(userId, familyId, "sibling-hash");
        var session = UserSession.Start(userId, familyId, "device");
        var user = CreateActiveUser();

        _refreshTokenHasher.Hash("hash").Returns("hash");
        _refreshTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(token);
        _refreshTokenRepository.GetFamilyAsync(familyId, Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken> { token, sibling });
        _userSessionRepository.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<UserSession> { session });
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _opaqueTokenGenerator.Generate().Returns("raw-reset-token");
        _refreshTokenHasher.Hash("raw-reset-token").Returns("reset-token-hash");

        var act = () => CreateHandler().Handle(
            new RefreshTokenCommand("hash"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();

        _ = _refreshTokenRepository.Received(1).GetFamilyAsync(familyId, Arg.Any<CancellationToken>());
        _ = _userSessionRepository.Received(1).GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>());

        sibling.IsActive.Should().BeFalse();
        session.IsActive.Should().BeFalse();
        _auditLogger.Received(1).Log("TOKEN_THEFT", userId.ToString(), Arg.Any<string>());
        await _emailSender.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        _passwordResetTokenRepository.Received(1).Add(Arg.Is<PasswordResetToken>(t => t != null && t.UserId == user.Id));
    }

    [Fact]
    public async Task Handle_throws_when_session_was_revoked()
    {
        var user = CreateActiveUser();
        var familyId = Guid.NewGuid();
        var token = IssueToken(user.Id, familyId, "hash");
        var session = UserSession.Start(user.Id, familyId, "device");
        session.Revoke();

        _refreshTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(token);
        _userSessionRepository.GetByRefreshTokenFamilyIdAsync(familyId, Arg.Any<CancellationToken>()).Returns(session);

        var act = () => CreateHandler().Handle(
            new RefreshTokenCommand("hash"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_throws_when_user_is_locked_out()
    {
        var user = CreateActiveUser();
        user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15));

        var familyId = Guid.NewGuid();
        var token = IssueToken(user.Id, familyId, "hash");
        var session = UserSession.Start(user.Id, familyId, "device");

        _refreshTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(token);
        _userSessionRepository.GetByRefreshTokenFamilyIdAsync(familyId, Arg.Any<CancellationToken>()).Returns(session);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var act = () => CreateHandler().Handle(
            new RefreshTokenCommand("hash"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }
}
