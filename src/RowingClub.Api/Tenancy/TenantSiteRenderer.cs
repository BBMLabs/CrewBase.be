using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using RowingClub.Identity.Application.Companies.GetCompanySite;

namespace RowingClub.Api.Tenancy;

/// <summary>
/// Tek bir HTML template'i ({{...}} yer tutucularıyla) firmanın verileriyle doldurur. Template
/// wwwroot/tenant-site.html'dedir; her firma aynı template'i kendi adı/iletişim bilgileriyle görür.
/// </summary>
public sealed class TenantSiteRenderer(IWebHostEnvironment environment)
{
    private readonly ConcurrentDictionary<string, string> _templateCache = new();

    public string Render(CompanySiteDto company)
    {
        var template = _templateCache.GetOrAdd("tenant-site.html", LoadTemplate);
        var encoder = HtmlEncoder.Default;

        return template
            .Replace("{{COMPANY_NAME}}", encoder.Encode(company.Name))
            .Replace("{{COMPANY_INITIAL}}", encoder.Encode(company.Name.Trim()[..1].ToUpperInvariant()))
            .Replace("{{SUBDOMAIN}}", encoder.Encode(company.Subdomain))
            .Replace("{{SITE_HOST}}", encoder.Encode($"{company.Subdomain}.{TenantResolver.BaseDomain}"))
            .Replace("{{PHONE}}", encoder.Encode(company.Phone ?? "-"))
            .Replace("{{EMAIL}}", encoder.Encode(company.ContactEmail ?? "-"))
            .Replace("{{ADDRESS}}", encoder.Encode(company.Address ?? "-"));
    }

    private string LoadTemplate(string fileName)
    {
        var fileInfo = environment.WebRootFileProvider.GetFileInfo(fileName);
        if (!fileInfo.Exists)
            throw new FileNotFoundException($"Site template'i bulunamadı: wwwroot/{fileName}");

        using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
