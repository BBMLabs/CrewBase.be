using FluentAssertions;
using RowingClub.Identity.Domain.Events;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.UnitTests.Identity.Domain;

public sealed class UserTests
{
    [Fact]
    public void Register_raises_UserRegisteredDomainEvent_with_normalized_email()
    {
        var user = User.Register(EmailAddress.Create("  Test@Example.com "));

        user.Email.Value.Should().Be("test@example.com");
        user.EmailVerified.Should().BeFalse();
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserRegisteredDomainEvent>();
    }

    [Fact]
    public void RegisterFailedLogin_locks_account_after_reaching_max_attempts()
    {
        var user = User.Register(EmailAddress.Create("test@example.com"));
        user.ClearDomainEvents();

        for (var i = 0; i < 4; i++)
        {
            user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
        }

        user.IsLockedOut().Should().BeFalse();

        user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        user.IsLockedOut().Should().BeTrue();
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserLockedOutDomainEvent>();
    }

    [Fact]
    public void RegisterSuccessfulLogin_resets_failed_attempt_counter_and_lock()
    {
        var user = User.Register(EmailAddress.Create("test@example.com"));
        for (var i = 0; i < 5; i++)
        {
            user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
        }

        user.IsLockedOut().Should().BeTrue();

        user.RegisterSuccessfulLogin();

        user.IsLockedOut().Should().BeFalse();
        user.FailedLoginAttemptCount.Should().Be(0);
    }

    [Fact]
    public void EnsureCanAuthenticate_throws_when_account_is_locked()
    {
        var user = User.Register(EmailAddress.Create("test@example.com"));
        for (var i = 0; i < 5; i++)
        {
            user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
        }

        var act = user.EnsureCanAuthenticate;

        act.Should().Throw<RowingClub.BuildingBlocks.Domain.DomainException>()
            .Which.ErrorCode.Should().Be("account_locked");
    }
}
