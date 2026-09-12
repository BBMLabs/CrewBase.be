using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Branches;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Instructors;

namespace RowingClub.Scheduling.Application.Panel;

/// <summary>Firma panelinden kendi paket durumu: limitler (ITenantDatabase üzerinden zaten çözülmüş) + güncel kullanım.</summary>
public sealed record GetCompanyPlanQuery : IRequest<CompanyPlanDto>;

public sealed record CompanyPlanDto(
    string Plan,
    int MaxBranches, int UsedBranches,
    int MaxMembers, int UsedMembers,
    int MaxBoats, int UsedBoats,
    int MaxInstructors, int UsedInstructors,
    int MaxManagers, int MaxEmployees,
    bool CanExportData,
    bool HasAdvancedReports, bool HasAutomaticDuesReminders);

public sealed class GetCompanyPlanQueryHandler(
    ITenantDatabase tenantDatabase,
    IBranchRepository branchRepository,
    ICustomerRepository customerRepository,
    IBoatRepository boatRepository,
    IInstructorRepository instructorRepository)
    : IRequestHandler<GetCompanyPlanQuery, CompanyPlanDto>
{
    public async Task<CompanyPlanDto> Handle(GetCompanyPlanQuery request, CancellationToken cancellationToken)
    {
        var usedBranches = await branchRepository.CountActiveAsync(cancellationToken);
        var usedMembers = await customerRepository.CountAsync(cancellationToken);
        var usedBoats = await boatRepository.CountActiveAsync(cancellationToken);
        var usedInstructors = await instructorRepository.CountActiveAsync(cancellationToken);

        return new CompanyPlanDto(
            tenantDatabase.Plan,
            tenantDatabase.MaxBranches, usedBranches,
            tenantDatabase.MaxMembers, usedMembers,
            tenantDatabase.MaxBoats, usedBoats,
            tenantDatabase.MaxInstructors, usedInstructors,
            tenantDatabase.MaxManagers, tenantDatabase.MaxEmployees,
            tenantDatabase.CanExportData,
            tenantDatabase.HasAdvancedReports, tenantDatabase.HasAutomaticDuesReminders);
    }
}
