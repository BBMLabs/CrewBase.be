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
            .Select(c =>
            {
                var limits = c.PlanLimits;
                return new CompanySiteDto(
                    c.Id, c.Name, c.Subdomain, c.DatabaseName,
                    c.Phone, c.ContactEmail, c.Address, c.TaxNumber, c.Status.ToString(),
                    c.Plan.ToString(), limits.MaxBranches, limits.MaxMembers, limits.MaxBoats);
            })
            .ToList();
    }
}
