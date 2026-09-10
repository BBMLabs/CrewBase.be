using System.Text;
using System.Text.RegularExpressions;

namespace RowingClub.Identity.Domain.Companies;

/// <summary>
/// Firma adından subdomain ve tenant veritabanı adı üretir:
/// "Öz Güven Kuaför" -> subdomain "oz-guven-kuafor", veritabanı "tenant_oz_guven_kuafor".
/// </summary>
public static partial class SubdomainSlug
{
    private const int MaxLength = 40;
    public const int UserSubdomainMinLength = 3;
    public const int UserSubdomainMaxLength = 63;

    private static readonly HashSet<string> ReservedSubdomains =
    [
        "www", "api", "admin", "app", "mail", "ftp", "master", "panel", "static", "cdn",
        "assets", "help", "support", "blog", "status", "docs", "dashboard", "test", "staging", "dev",
    ];

    public static bool IsValidUserSubdomain(string subdomain) =>
        subdomain.Length is >= UserSubdomainMinLength and <= UserSubdomainMaxLength
        && UserSubdomainPattern().IsMatch(subdomain);

    public static bool IsReserved(string subdomain) => ReservedSubdomains.Contains(subdomain);

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$")]
    private static partial Regex UserSubdomainPattern();

    public static string FromCompanyName(string companyName)
    {
        var builder = new StringBuilder(companyName.Length);
        var previousWasDash = true; // baştaki tireleri engeller

        foreach (var rawChar in companyName.Trim().ToLowerInvariant())
        {
            var ch = rawChar switch
            {
                'ç' => 'c',
                'ğ' => 'g',
                'ı' => 'i',
                'ö' => 'o',
                'ş' => 's',
                'ü' => 'u',
                _ => rawChar,
            };

            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(ch);
                previousWasDash = false;
            }
            else if (!previousWasDash)
            {
                builder.Append('-');
                previousWasDash = true;
            }

            if (builder.Length >= MaxLength)
                break;
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length == 0 ? "firma" : slug;
    }

    public static string ToDatabaseName(string subdomain) =>
        "tenant_" + subdomain.Replace('-', '_');
}
