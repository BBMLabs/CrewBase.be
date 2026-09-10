using FluentAssertions;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.UnitTests.Identity.Domain;

public sealed class SubdomainSlugTests
{
    [Theory]
    [InlineData("deniz-kurek")]
    [InlineData("a23")]
    public void IsValidUserSubdomain_accepts_well_formed_values(string value)
    {
        SubdomainSlug.IsValidUserSubdomain(value).Should().BeTrue();
    }

    [Fact]
    public void IsValidUserSubdomain_accepts_value_at_max_length()
    {
        var atMaxLength = new string('a', SubdomainSlug.UserSubdomainMaxLength);

        SubdomainSlug.IsValidUserSubdomain(atMaxLength).Should().BeTrue();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("-deniz")]
    [InlineData("deniz-")]
    [InlineData("Deniz-Kurek")]
    [InlineData("deniz_kurek")]
    [InlineData("deniz kurek")]
    [InlineData("")]
    public void IsValidUserSubdomain_rejects_malformed_values(string value)
    {
        SubdomainSlug.IsValidUserSubdomain(value).Should().BeFalse();
    }

    [Fact]
    public void IsValidUserSubdomain_rejects_values_over_max_length()
    {
        var tooLong = new string('a', 64);

        SubdomainSlug.IsValidUserSubdomain(tooLong).Should().BeFalse();
    }

    [Theory]
    [InlineData("www")]
    [InlineData("admin")]
    [InlineData("api")]
    public void IsReserved_flags_reserved_words(string value)
    {
        SubdomainSlug.IsReserved(value).Should().BeTrue();
    }

    [Fact]
    public void IsReserved_allows_non_reserved_word()
    {
        SubdomainSlug.IsReserved("deniz-kurek").Should().BeFalse();
    }
}
