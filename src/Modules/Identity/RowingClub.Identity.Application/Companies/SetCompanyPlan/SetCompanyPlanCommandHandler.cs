using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Companies.SetCompanyPlan;

public sealed class SetCompanyPlanCommandHandler(
    ICompanyRepository companyRepository, IAuditLogger auditLogger)
    : IRequestHandler<SetCompanyPlanCommand, Unit>
{
    public async Task<Unit> Handle(SetCompanyPlanCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        if (!Enum.TryParse<CompanyPlan>(request.Plan, ignoreCase: true, out var plan))
            throw new DomainException("invalid_plan", "Geçersiz paket.");

        company.SetPlan(
            plan, request.CustomMaxBranches, request.CustomMaxMembers, request.CustomMaxBoats,
            request.CustomMaxInstructors, request.CustomMaxManagers, request.CustomMaxEmployees,
            request.CustomCanExportData, request.CustomHasAdvancedReports, request.CustomHasAutomaticDuesReminders);
        companyRepository.Update(company);

        auditLogger.Log("COMPANY_PLAN_SET", company.Id.ToString(), $"Master admin paketi ayarladı: {plan}");

        return Unit.Value;
    }
}
