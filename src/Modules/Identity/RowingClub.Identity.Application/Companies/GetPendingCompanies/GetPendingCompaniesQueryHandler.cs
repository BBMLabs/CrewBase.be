using MediatR;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Companies.GetPendingCompanies;

public sealed class GetPendingCompaniesQueryHandler(
    ICompanyRepository companyRepository)
    : IRequestHandler<GetPendingCompaniesQuery, List<PendingCompanyDto>>
{
    public async Task<List<PendingCompanyDto>> Handle(
        GetPendingCompaniesQuery request, CancellationToken cancellationToken)
    {
        var pending = await companyRepository.GetByStatusAsync(
            CompanyStatus.PendingApproval, cancellationToken);

        return pending.Select(c => new PendingCompanyDto(
            c.Id, c.Name, c.ContactEmail, c.Phone, c.Address, c.CreatedAtUtc)).ToList();
    }
}
