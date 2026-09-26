using System.Globalization;
using System.Text;
using System.Xml;

namespace RowingClub.Api.Tenancy;

public static class ClubSiteSeo
{
    public const string CacheControlValue = "public, max-age=3600";

    public static readonly IReadOnlyList<string> StaticPaths =
        ["/", "/about", "/packages", "/gallery", "/contact", "/book", "/privacy"];

    public static string SiteOrigin(string subdomain) => $"https://{subdomain}.{TenantResolver.BaseDomain}";

    public static void ApplyCacheHeaders(HttpContext httpContext)
    {
        httpContext.Response.Headers.CacheControl = CacheControlValue;
    }

    public static string BuildSitemap(string siteOrigin, IEnumerable<string> branchCodes, DateOnly lastModified)
    {
        var paths = StaticPaths.Concat(
            branchCodes
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(code => $"/branch/{Uri.EscapeDataString(code)}"));

        var lastModifiedText = lastModified.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            OmitXmlDeclaration = false,
        };

        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            foreach (var path in paths)
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", siteOrigin + path);
                writer.WriteElementString("lastmod", lastModifiedText);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static string BuildRobots(string siteOrigin, bool allowIndexing)
    {
        var builder = new StringBuilder();
        builder.Append("User-agent: *\n");
        if (allowIndexing)
        {
            builder.Append("Disallow: /member/\n");
            builder.Append("Disallow: /rsvp\n");
        }
        else
        {
            builder.Append("Disallow: /\n");
        }

        builder.Append('\n');
        builder.Append($"Sitemap: {siteOrigin}/sitemap.xml\n");
        return builder.ToString();
    }
}
