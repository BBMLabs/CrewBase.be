using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Companies.RestoreCompany;

public sealed class RestoreCompanyCommandHandler(
    ICompanyRepository companyRepository, IAuditLogger auditLogger)
    : IRequestHandler<RestoreCompanyCommand, Unit>
{
    public async Task<Unit> Handle(RestoreCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        company.Restore();
        companyRepository.Update(company);

        auditLogger.Log("COMPANY_RESTORED", company.Id.ToString(), $"Şirket geri yüklendi: {company.Name}");

        return Unit.Value;
    }
}
