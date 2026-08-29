using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Identity.Application.Companies.GetCompanySite;

namespace RowingClub.Api.Tenancy;

/// <summary>
/// Public site istekleri için subdomain'den, panel istekleri için oturumdaki CompanyId'den firmayı
/// bulur ve <see cref="ITenantDatabase"/>'i doldurur; ondan sonrası (repository'ler) firmanın kendi
/// veritabanına gider. Domain şimdilik mock: subdomain URL path'inden ({subdomain}) gelir, gerçek
/// {subdomain}.faturebase.com host'ları da <see cref="FromHost"/> ile desteklenir.
/// </summary>
public sealed class TenantResolver(ISender sender, ITenantDatabase tenantDatabase)
{
    public const string BaseDomain = "faturebase.com";

    public async Task<CompanySiteDto?> ResolveBySubdomainAsync(
        string subdomain, CancellationToken cancellationToken)
    {
        var company = await sender.Send(new GetCompanySiteQuery(subdomain, null), cancellationToken);
        if (company is not null)
            tenantDatabase.Set(
                company.CompanyId, company.DatabaseName, company.Subdomain, company.Plan,
                company.MaxBranches, company.MaxMembers, company.MaxBoats, company.MaxInstructors,
                company.MaxManagers, company.MaxEmployees, company.CanExportData,
                company.HasAdvancedReports, company.HasAutomaticDuesReminders);

        return company;
    }

    public async Task<CompanySiteDto?> ResolveByCompanyIdAsync(
        Guid companyId, CancellationToken cancellationToken)
    {
        var company = await sender.Send(new GetCompanySiteQuery(null, companyId), cancellationToken);
        if (company is not null)
            tenantDatabase.Set(
                company.CompanyId, company.DatabaseName, company.Subdomain, company.Plan,
                company.MaxBranches, company.MaxMembers, company.MaxBoats, company.MaxInstructors,
                company.MaxManagers, company.MaxEmployees, company.CanExportData,
                company.HasAdvancedReports, company.HasAutomaticDuesReminders);

        return company;
    }

    /// <summary>
    /// "xyz.faturebase.com" -> "xyz". Geliştirmede "xyz.localhost" da desteklenir (tarayıcılar
    /// *.localhost'u 127.0.0.1'e çözer), böylece subdomain akışı gerçek DNS olmadan denenebilir.
    /// Subdomain yoksa (ana domain, çıplak localhost, IP) null.
    /// </summary>
    public static string? FromHost(string host)
    {
        if (string.IsNullOrEmpty(host))
            return null;

        var hostName = host.Split(':')[0].ToLowerInvariant();

        string? subdomain = null;
        if (hostName.EndsWith("." + BaseDomain, StringComparison.Ordinal))
            subdomain = hostName[..^(BaseDomain.Length + 1)];
        else if (hostName.EndsWith(".localhost", StringComparison.Ordinal))
            subdomain = hostName[..^".localhost".Length];

        return string.IsNullOrEmpty(subdomain) || subdomain.Contains('.') || subdomain == "www"
            ? null
            : subdomain;
    }
}
