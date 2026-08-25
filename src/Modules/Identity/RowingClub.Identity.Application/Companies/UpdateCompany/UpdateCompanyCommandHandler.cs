using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Companies.UpdateCompany;

public sealed class UpdateCompanyCommandHandler(ICompanyRepository companyRepository)
    : IRequestHandler<UpdateCompanyCommand, Unit>
{
    public async Task<Unit> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        company.UpdateDetails(request.Name, request.Phone, request.ContactEmail, request.Address, request.TaxNumber);
        companyRepository.Update(company);

        return Unit.Value;
    }
}
