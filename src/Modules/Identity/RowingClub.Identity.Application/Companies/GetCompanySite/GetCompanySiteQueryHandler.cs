using MediatR;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Companies.GetCompanySite;

public sealed class GetCompanySiteQueryHandler(ICompanyRepository companyRepository)
    : IRequestHandler<GetCompanySiteQuery, CompanySiteDto?>
{
    public async Task<CompanySiteDto?> Handle(GetCompanySiteQuery request, CancellationToken cancellationToken)
    {
        var company = request switch
        {
            { CompanyId: { } id } => await companyRepository.GetByIdAsync(id, cancellationToken),
            { Subdomain: { } subdomain } => await companyRepository.GetBySubdomainAsync(
                subdomain.Trim().ToLowerInvariant(), cancellationToken),
            _ => null,
        };

        if (company is null || company.Status != CompanyStatus.Active)
            return null;

        var limits = company.PlanLimits;
        var features = company.PlanFeatures;
        return new CompanySiteDto(
            company.Id, company.Name, company.Subdomain, company.DatabaseName,
            company.Phone, company.ContactEmail, company.Address, company.TaxNumber, company.Status.ToString(),
            company.Plan.ToString(), limits.MaxBranches, limits.MaxMembers, limits.MaxBoats, limits.MaxInstructors,
            features.MaxManagers, features.MaxEmployees, features.CanExportData,
            features.HasAdvancedReports, features.HasAutomaticDuesReminders);
    }
}
