using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Platform;

public sealed record GetTenantDatabaseOverviewQuery : IRequest<List<TenantDatabaseOverviewDto>>;

public sealed record TenantDatabaseOverviewDto(
    string DatabaseName,
    long SizeBytes,
    Guid? CompanyId,
    string? CompanyName,
    string? Subdomain,
    string? Status,
    DateTimeOffset? CreatedAtUtc,
    bool IsOrphan,
    int? MemberCount,
    int? AppointmentCount,
    bool UsageStatsAvailable);

public sealed class GetTenantDatabaseOverviewQueryHandler(
    ICompanyRepository companyRepository,
    ITenantDatabaseProvisioner tenantDatabaseProvisioner)
    : IRequestHandler<GetTenantDatabaseOverviewQuery, List<TenantDatabaseOverviewDto>>
{
    public async Task<List<TenantDatabaseOverviewDto>> Handle(
        GetTenantDatabaseOverviewQuery request, CancellationToken cancellationToken)
    {
        var sizesByDatabaseName = await tenantDatabaseProvisioner.GetDatabaseSizesAsync(cancellationToken);
        var companies = await companyRepository.GetAllAsync(cancellationToken);
        var companyByDatabaseName = companies.ToDictionary(c => c.DatabaseName, c => c);

        var overview = new List<TenantDatabaseOverviewDto>();
        foreach (var (databaseName, sizeBytes) in sizesByDatabaseName)
        {
            var usageStats = await tenantDatabaseProvisioner.GetUsageStatsAsync(databaseName, cancellationToken);
            companyByDatabaseName.TryGetValue(databaseName, out var company);

            overview.Add(new TenantDatabaseOverviewDto(
                databaseName,
                sizeBytes,
                company?.Id,
                company?.Name,
                company?.Subdomain,
                company?.Status.ToString(),
                company?.CreatedAtUtc,
                company is null,
                usageStats.MemberCount,
                usageStats.AppointmentCount,
                usageStats.Available));
        }

        return overview.OrderByDescending(dto => dto.SizeBytes).ToList();
    }
}
