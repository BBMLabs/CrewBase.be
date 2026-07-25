using FluentAssertions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.UnitTests.Identity.Domain;

public sealed class UserRecoveryCodeTests
{
    [Fact]
    public void Create_sets_properties_correctly()
    {
        var code = UserRecoveryCode.Create(Guid.NewGuid(), "hashed-value");

        code.IsUsed.Should().BeFalse();
        code.UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkUsed_sets_used_flag_and_timestamp()
    {
        var code = UserRecoveryCode.Create(Guid.NewGuid(), "hashed-value");
        code.MarkUsed();

        code.IsUsed.Should().BeTrue();
        code.UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MarkUsed_is_idempotent()
    {
        var code = UserRecoveryCode.Create(Guid.NewGuid(), "hashed-value");
        code.MarkUsed();
        code.MarkUsed();

        code.IsUsed.Should().BeTrue();
    }
}

public sealed class UserLockoutTests
{
    [Fact]
    public void RegisterFailedLogin_locks_account_after_max_attempts()
    {
        var user = User.Register(EmailAddress.Create("test@example.com"));

        for (int i = 0; i < 5; i++)
            user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15));

        user.IsLockedOut().Should().BeTrue();
        user.FailedLoginAttemptCount.Should().Be(5);
    }

    [Fact]
    public void RegisterSuccessfulLogin_resets_failed_attempts_and_lockout()
    {
        var user = User.Register(EmailAddress.Create("test@example.com"));
        for (int i = 0; i < 3; i++)
            user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15));

        user.RegisterSuccessfulLogin();

        user.FailedLoginAttemptCount.Should().Be(0);
        user.IsLockedOut().Should().BeFalse();
    }

    [Fact(Skip = "Deactivated status is only reachable through domain behavior not exposed in tests yet. "
        + "Remove Skip when a domain method for deactivation is added.")]
    public void EnsureCanAuthenticate_throws_when_account_deactivated()
    {
        // Placeholder: When User exposes a Deactivate() method, use it here:
        // var user = User.Register(...);
        // user.Deactivate();
        // var act = () => user.EnsureCanAuthenticate();
        // act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("account_deactivated");
    }

    [Fact]
    public void EnsureCanAuthenticate_throws_generic_message_for_locked_account()
    {
        var user = User.Register(EmailAddress.Create("test@example.com"));
        for (int i = 0; i < 5; i++)
            user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15));

        var act = () => user.EnsureCanAuthenticate();

        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("account_locked");
    }
}
