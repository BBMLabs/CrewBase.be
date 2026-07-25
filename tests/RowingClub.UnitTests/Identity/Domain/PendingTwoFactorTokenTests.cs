using FluentAssertions;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.UnitTests.Identity.Domain;

public sealed class PendingTwoFactorTokenTests
{
    [Fact]
    public void Create_sets_expiry_from_lifetime()
    {
        var token = PendingTwoFactorToken.Create(Guid.NewGuid(), "hash", "device", TimeSpan.FromMinutes(5));

        token.ExpiresAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(2));
        token.IsUsed.Should().BeFalse();
    }

    [Fact]
    public void IsValid_returns_true_when_not_used_and_not_expired()
    {
        var token = PendingTwoFactorToken.Create(Guid.NewGuid(), "hash", "device", TimeSpan.FromMinutes(5));

        token.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_returns_false_when_expired()
    {
        var token = PendingTwoFactorToken.Create(Guid.NewGuid(), "hash", "device", TimeSpan.FromMinutes(-1));

        token.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_returns_false_when_already_used()
    {
        var token = PendingTwoFactorToken.Create(Guid.NewGuid(), "hash", "device", TimeSpan.FromMinutes(5));
        token.MarkUsed();

        token.IsValid.Should().BeFalse();
    }

    [Fact]
    public void MarkUsed_can_only_be_called_once()
    {
        var token = PendingTwoFactorToken.Create(Guid.NewGuid(), "hash", "device", TimeSpan.FromMinutes(5));
        token.MarkUsed();
        token.MarkUsed(); // should not throw

        token.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void Is_not_a_jwt_token()
    {
        var token = PendingTwoFactorToken.Create(Guid.NewGuid(), "hash", "device", TimeSpan.FromMinutes(5));

        // Pending token is a plain POCO (no JWT base class, no claims properties) - it is only
        // an opaque reference checked server-side during /verify-2fa. It must never be a bearer
        // token that can be used to access protected endpoints.
        typeof(PendingTwoFactorToken).BaseType.Should().Be(typeof(object));
        typeof(PendingTwoFactorToken).GetProperties().Select(p => p.Name)
            .Should().NotContain(new[] { "sub", "role", "company_id" });
    }
}
