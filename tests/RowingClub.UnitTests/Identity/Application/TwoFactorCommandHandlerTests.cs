using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Collections.Generic;
using System.Security.Claims;
using RowingClub.BuildingBlocks.Security.Jwt;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Application.Login;
using RowingClub.Identity.Application.Tokens;
using RowingClub.Identity.Application.TwoFactor;
using RowingClub.Identity.Application.TwoFactor.GenerateRecoveryCodes;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.UnitTests.Identity.Application;

public sealed class GenerateRecoveryCodesCommandHandlerTests
{
    private readonly IRecoveryCodeRepository _recoveryCodeRepository = Substitute.For<IRecoveryCodeRepository>();
    private readonly IOpaqueTokenGenerator _opaqueTokenGenerator = Substitute.For<IOpaqueTokenGenerator>();
    private readonly IRefreshTokenHasher _refreshTokenHasher = Substitute.For<IRefreshTokenHasher>();

    private GenerateRecoveryCodesCommandHandler CreateHandler() =>
        new(_recoveryCodeRepository, _opaqueTokenGenerator, _refreshTokenHasher);

    [Fact]
    public async Task Handle_generates_10_codes_and_marks_old_ones_used()
    {
        var userId = Guid.NewGuid();
        var oldCodes = new List<UserRecoveryCode>
        {
            UserRecoveryCode.Create(userId, "old-hash-1"),
            UserRecoveryCode.Create(userId, "old-hash-2"),
        };

        _recoveryCodeRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(oldCodes);
        _opaqueTokenGenerator.Generate().Returns("raw-1", "raw-2", "raw-3", "raw-4", "raw-5",
            "raw-6", "raw-7", "raw-8", "raw-9", "raw-10");
        _refreshTokenHasher.Hash(Arg.Any<string>()).Returns("hashed-code");

        var response = await CreateHandler().Handle(
            new GenerateRecoveryCodesCommand(userId), CancellationToken.None);

        response.RecoveryCodes.Should().HaveCount(10);
        oldCodes.All(c => c.IsUsed).Should().BeTrue();
        _recoveryCodeRepository.Received(1).AddRange(Arg.Is<IEnumerable<UserRecoveryCode>>(codes =>
            codes!.Count() == 10));
    }
}

public sealed class VerifyTwoFactorLoginCommandHandlerTests
{
    private readonly IPendingTwoFactorTokenRepository _pendingTokenRepository = Substitute.For<IPendingTwoFactorTokenRepository>();
    private readonly IRefreshTokenHasher _refreshTokenHasher = Substitute.For<IRefreshTokenHasher>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserSessionRepository _userSessionRepository = Substitute.For<IUserSessionRepository>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IOpaqueTokenGenerator _opaqueTokenGenerator = Substitute.For<IOpaqueTokenGenerator>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
    private readonly IRecoveryCodeRepository _recoveryCodeRepository = Substitute.For<IRecoveryCodeRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();

    private VerifyTwoFactorLoginCommandHandler CreateHandler()
    {
        var tokenPairIssuer = new TokenPairIssuer(
            _jwtTokenService, _opaqueTokenGenerator, _refreshTokenHasher,
            _refreshTokenRepository, Options.Create(new IdentityOptions
            {
                RefreshTokenLifetime = TimeSpan.FromDays(30),
            }));

        return new VerifyTwoFactorLoginCommandHandler(
            _pendingTokenRepository, _refreshTokenHasher, _userRepository,
            _userSessionRepository, tokenPairIssuer, _auditLogger,
            _recoveryCodeRepository, _emailSender);
    }

    private static User CreateUserWithTotp()
    {
        var user = User.Register(EmailAddress.Create("user@example.com"));
        user.EnableTwoFactor("Totp", TwoFactorService.GenerateTotpSecret());
        return user;
    }

    [Fact]
    public async Task Handle_throws_when_pending_token_not_found()
    {
        _refreshTokenHasher.Hash("unknown-token").Returns("unknown-hash");
        _pendingTokenRepository.GetByTokenHashAsync("unknown-hash", Arg.Any<CancellationToken>())
            .Returns((PendingTwoFactorToken?)null);

        var act = () => CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand("unknown-token", "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_throws_when_pending_token_is_expired()
    {
        var userId = Guid.NewGuid();
        var pendingToken = PendingTwoFactorToken.Create(userId, "hash", "device", TimeSpan.FromMinutes(-5));

        _pendingTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(pendingToken);
        _refreshTokenHasher.Hash("raw-token").Returns("hash");

        var act = () => CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand("raw-token", "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_throws_when_pending_token_already_used()
    {
        var userId = Guid.NewGuid();
        var pendingToken = PendingTwoFactorToken.Create(userId, "hash", "device", TimeSpan.FromMinutes(5));
        pendingToken.MarkUsed();

        _pendingTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(pendingToken);
        _refreshTokenHasher.Hash("raw-token").Returns("hash");

        var act = () => CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand("raw-token", "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_throws_when_totp_code_is_wrong()
    {
        var user = CreateUserWithTotp();
        var pendingToken = PendingTwoFactorToken.Create(user.Id, "hash", "device", TimeSpan.FromMinutes(5));

        _pendingTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(pendingToken);
        _refreshTokenHasher.Hash("raw-token").Returns("hash");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _recoveryCodeRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new List<UserRecoveryCode>());

        var act = () => CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand("raw-token", "000000"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_uses_recovery_code_when_totp_code_is_wrong_and_returns_remaining_count()
    {
        var user = CreateUserWithTotp();
        var pendingToken = PendingTwoFactorToken.Create(user.Id, "hash", "device", TimeSpan.FromMinutes(5));
        var recoveryCode = UserRecoveryCode.Create(user.Id, "recovery-hash");

        _pendingTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(pendingToken);
        _refreshTokenHasher.Hash("raw-token").Returns("hash");
        _refreshTokenHasher.Hash("valid-recovery-code").Returns("recovery-hash");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _recoveryCodeRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new List<UserRecoveryCode> { recoveryCode });
        _recoveryCodeRepository.GetUnusedCountAsync(user.Id, Arg.Any<CancellationToken>()).Returns(5);
        _opaqueTokenGenerator.Generate().Returns("raw-refresh");
        _refreshTokenHasher.Hash("raw-refresh").Returns("refresh-hash");
        _jwtTokenService.IssueAccessToken(user.Id, user.Email.Value, Arg.Any<IReadOnlyCollection<Claim>?>())
            .Returns(new IssuedAccessToken("access-token", DateTimeOffset.UtcNow.AddMinutes(15), "jti"));

        var response = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand("raw-token", "valid-recovery-code"), CancellationToken.None);

        response.RemainingRecoveryCodes.Should().Be(5);
        recoveryCode.IsUsed.Should().BeTrue();
        pendingToken.IsValid.Should().BeFalse();
        _auditLogger.Received(1).Log("2FA_RECOVERY_USED", user.Id.ToString(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_sends_email_when_recovery_codes_run_low()
    {
        var user = CreateUserWithTotp();
        var pendingToken = PendingTwoFactorToken.Create(user.Id, "hash", "device", TimeSpan.FromMinutes(5));
        var recoveryCode = UserRecoveryCode.Create(user.Id, "recovery-hash");

        _pendingTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(pendingToken);
        _refreshTokenHasher.Hash("raw-token").Returns("hash");
        _refreshTokenHasher.Hash("last-code").Returns("recovery-hash");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _recoveryCodeRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new List<UserRecoveryCode> { recoveryCode });
        _recoveryCodeRepository.GetUnusedCountAsync(user.Id, Arg.Any<CancellationToken>()).Returns(2);
        _opaqueTokenGenerator.Generate().Returns("raw-refresh");
        _refreshTokenHasher.Hash("raw-refresh").Returns("refresh-hash");
        _jwtTokenService.IssueAccessToken(user.Id, user.Email.Value, Arg.Any<IReadOnlyCollection<Claim>?>())
            .Returns(new IssuedAccessToken("access-token", DateTimeOffset.UtcNow.AddMinutes(15), "jti"));

        await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand("raw-token", "last-code"), CancellationToken.None);

        await _emailSender.Received(1).SendAsync(Arg.Is<EmailMessage>(m =>
            m!.To == user.Email.Value && m.Subject.Contains("Kurtarma")), Arg.Any<CancellationToken>());
    }
}
