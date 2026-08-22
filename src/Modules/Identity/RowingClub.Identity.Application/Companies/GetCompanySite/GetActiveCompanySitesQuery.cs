using MediatR;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Companies.GetCompanySite;

/// <summary>Hatırlatma servisi ve açılıştaki tenant migration taraması için aktif firmalar.</summary>
public sealed record GetActiveCompanySitesQuery : IRequest<List<CompanySiteDto>>;

public sealed class GetActiveCompanySitesQueryHandler(ICompanyRepository companyRepository)
    : IRequestHandler<GetActiveCompanySitesQuery, List<CompanySiteDto>>
{
    public async Task<List<CompanySiteDto>> Handle(
        GetActiveCompanySitesQuery request, CancellationToken cancellationToken)
    {
        var companies = await companyRepository.GetByStatusAsync(CompanyStatus.Active, cancellationToken);

        return companies
            .Select(c => new CompanySiteDto(
                c.Id, c.Name, c.Subdomain, c.DatabaseName,
                c.Phone, c.ContactEmail, c.Address, c.Status.ToString()))
            .ToList();
    }
}
