using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Companies.SoftDeleteCompany;

public sealed class SoftDeleteCompanyCommandHandler(
    ICompanyRepository companyRepository, IAuditLogger auditLogger)
    : IRequestHandler<SoftDeleteCompanyCommand, Unit>
{
    public async Task<Unit> Handle(SoftDeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        company.Delete();
        companyRepository.Update(company);

        auditLogger.Log("COMPANY_SOFT_DELETED", company.Id.ToString(), $"Şirket silindi (soft delete): {company.Name}");

        return Unit.Value;
    }
}
