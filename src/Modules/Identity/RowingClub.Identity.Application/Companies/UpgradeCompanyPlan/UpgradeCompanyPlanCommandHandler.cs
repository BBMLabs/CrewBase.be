using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Companies.UpgradeCompanyPlan;

public sealed class UpgradeCompanyPlanCommandHandler(
    ICompanyRepository companyRepository, IAuditLogger auditLogger)
    : IRequestHandler<UpgradeCompanyPlanCommand, UpgradeCompanyPlanResult>
{
    public async Task<UpgradeCompanyPlanResult> Handle(
        UpgradeCompanyPlanCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        if (!Enum.TryParse<CompanyPlan>(request.Plan, ignoreCase: true, out var newPlan))
            throw new DomainException("invalid_plan", "Geçersiz paket.");

        company.ChangePlan(newPlan, request.UsedBranches, request.UsedMembers, request.UsedBoats);
        companyRepository.Update(company);

        auditLogger.Log("COMPANY_PLAN_CHANGED", company.Id.ToString(), $"Firma paketini değiştirdi: {newPlan}");

        var limits = company.PlanLimits;
        return new UpgradeCompanyPlanResult(company.Plan.ToString(), limits.MaxBranches, limits.MaxMembers, limits.MaxBoats);
    }
}
