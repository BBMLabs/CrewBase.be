using System.Xml.Linq;
using FluentAssertions;
using RowingClub.Api.Tenancy;

namespace RowingClub.SecurityTests;

public sealed class ClubSiteSeoTests
{
    private const string Origin = "https://demo.faturebase.com";

    [Fact]
    public void SiteOrigin_uses_the_tenant_subdomain_on_the_base_domain()
    {
        ClubSiteSeo.SiteOrigin("demo").Should().Be(Origin);
    }

    [Fact]
    public void Robots_blocks_member_and_rsvp_areas_and_points_to_the_sitemap()
    {
        var robots = ClubSiteSeo.BuildRobots(Origin, allowIndexing: true);

        robots.Should().StartWith("User-agent: *\n");
        robots.Should().Contain("Disallow: /member/\n");
        robots.Should().Contain("Disallow: /rsvp\n");
        robots.Should().NotContain("Disallow: /\n");
        robots.Should().Contain($"Sitemap: {Origin}/sitemap.xml\n");
    }

    [Fact]
    public void Robots_disallows_everything_when_indexing_is_turned_off()
    {
        var robots = ClubSiteSeo.BuildRobots(Origin, allowIndexing: false);

        robots.Should().Contain("Disallow: /\n");
        robots.Should().Contain($"Sitemap: {Origin}/sitemap.xml\n");
    }

    [Fact]
    public void Sitemap_lists_static_pages_and_each_branch_with_absolute_urls()
    {
        var xml = ClubSiteSeo.BuildSitemap(
            Origin, ["moda", "bebek", "moda", "a&b"], new DateOnly(2026, 9, 24));

        var document = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = document.Root!.Elements(ns + "url").ToList();
        var locations = urls.Select(u => u.Element(ns + "loc")!.Value).ToList();

        locations.Should().Equal(
            $"{Origin}/", $"{Origin}/about", $"{Origin}/packages", $"{Origin}/gallery", $"{Origin}/contact",
            $"{Origin}/book", $"{Origin}/privacy", $"{Origin}/branch/moda", $"{Origin}/branch/bebek",
            $"{Origin}/branch/a%26b");
        urls.Should().OnlyContain(u => u.Element(ns + "lastmod")!.Value == "2026-09-24");
    }
}
