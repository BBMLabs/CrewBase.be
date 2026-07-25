using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Companies.SuspendCompany;

public sealed class SuspendCompanyCommandHandler(
    ICompanyRepository companyRepository)
    : IRequestHandler<SuspendCompanyCommand, Unit>
{
    public async Task<Unit> Handle(SuspendCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        company.Suspend();
        companyRepository.Update(company);

        return Unit.Value;
    }
}
