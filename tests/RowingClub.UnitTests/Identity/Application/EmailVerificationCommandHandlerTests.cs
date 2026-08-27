using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Email;
using RowingClub.Identity.Application.EmailVerification;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.UnitTests.Identity.Application;

public sealed class VerifyEmailCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailVerificationTokenRepository _tokenRepository = Substitute.For<IEmailVerificationTokenRepository>();
    private readonly IRefreshTokenHasher _hasher = Substitute.For<IRefreshTokenHasher>();
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly IOpaqueTokenGenerator _tokenGenerator = Substitute.For<IOpaqueTokenGenerator>();
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

    private VerifyEmailCommandHandler CreateHandler() =>
        new(_userRepository, _tokenRepository, _hasher, _companyRepository, _tokenGenerator,
            _passwordResetTokenRepository, _emailSender, _auditLogger, _configuration);

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

    [Fact]
    public async Task Handle_sends_activation_email_when_verifying_a_company_admin()
    {
        var companyId = Guid.NewGuid();
        var user = User.RegisterCompanyAdmin(EmailAddress.Create("admin@example.com"), companyId);
        var token = EmailVerificationToken.Issue(user.Id, "hashed-token", TimeSpan.FromHours(24));
        var company = Company.Register("Rowing Club", "rowing-club", "tenant_rowing_club", null, null, null, null);

        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _tokenRepository.GetByTokenHashAsync("hashed-token", Arg.Any<CancellationToken>()).Returns(token);
        _hasher.Hash("raw-token").Returns("hashed-token");
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _tokenGenerator.Generate().Returns("raw-activation-token");
        _hasher.Hash("raw-activation-token").Returns("hashed-activation-token");

        await CreateHandler().Handle(
            new VerifyEmailCommand("admin@example.com", "raw-token"), CancellationToken.None);

        user.EmailVerified.Should().BeTrue();
        _passwordResetTokenRepository.Received(1).Add(Arg.Is<PasswordResetToken>(t => t!.UserId == user.Id));
        await _emailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m!.To == "admin@example.com"), Arg.Any<CancellationToken>());
    }
}
