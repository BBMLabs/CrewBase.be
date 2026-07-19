using FluentAssertions;
using RowingClub.Identity.Domain.Events;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.UnitTests.Identity.Domain;

public sealed class RefreshTokenTests
{
    [Fact]
    public void IssueInFamily_keeps_the_same_family_id_as_the_original()
    {
        var userId = Guid.NewGuid();
        var original = RefreshToken.IssueNewFamily(userId, "hash-1", TimeSpan.FromDays(30));

        var rotated = RefreshToken.IssueInFamily(userId, original.FamilyId, "hash-2", TimeSpan.FromDays(30));

        rotated.FamilyId.Should().Be(original.FamilyId);
        rotated.Id.Should().NotBe(original.Id);
    }

    [Fact]
    public void MarkReplacedBy_revokes_the_token_and_records_the_successor()
    {
        var token = RefreshToken.IssueNewFamily(Guid.NewGuid(), "hash-1", TimeSpan.FromDays(30));
        var successorId = Guid.NewGuid();

        token.MarkReplacedBy(successorId);

        token.IsActive.Should().BeFalse();
        token.ReplacedByTokenId.Should().Be(successorId);
    }

    [Fact]
    public void FlagReuse_raises_RefreshTokenReuseDetectedDomainEvent()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.IssueNewFamily(userId, "hash-1", TimeSpan.FromDays(30));
        token.ClearDomainEvents();

        token.FlagReuse();

        token.IsActive.Should().BeFalse();
        token.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RefreshTokenReuseDetectedDomainEvent>()
            .Which.UserId.Should().Be(userId);
    }

    [Fact]
    public void IsActive_is_false_once_expired_even_without_explicit_revocation()
    {
        var token = RefreshToken.IssueNewFamily(Guid.NewGuid(), "hash-1", TimeSpan.FromSeconds(-1));

        token.IsActive.Should().BeFalse();
    }
}
