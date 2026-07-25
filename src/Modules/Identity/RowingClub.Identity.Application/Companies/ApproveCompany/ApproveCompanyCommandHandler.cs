using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Application.Companies.ApproveCompany;

public sealed class ApproveCompanyCommandHandler(
    ICompanyRepository companyRepository,
    IUserRepository userRepository)
    : IRequestHandler<ApproveCompanyCommand, Unit>
{
    public async Task<Unit> Handle(ApproveCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        var adminUser = await userRepository.GetByIdAsync(request.ApprovedByUserId, cancellationToken)
            ?? throw new DomainException("user_not_found", "Kullanıcı bulunamadı.");

        company.Approve(adminUser.Id);
        companyRepository.Update(company);

        return Unit.Value;
    }
}
