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

        return new CompanySiteDto(
            company.Id, company.Name, company.Subdomain, company.DatabaseName,
            company.Phone, company.ContactEmail, company.Address, company.Status.ToString());
    }
}
