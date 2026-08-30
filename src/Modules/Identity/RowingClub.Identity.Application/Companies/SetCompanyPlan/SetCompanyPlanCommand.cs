using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.SetCompanyPlan;

/// <summary>Master panelden serbest paket ataması (Custom dahil); Custom seçildiğinde özel limitler zorunludur.</summary>
public sealed record SetCompanyPlanCommand(
    Guid CompanyId, string Plan, int? CustomMaxBranches, int? CustomMaxMembers, int? CustomMaxBoats,
    int? CustomMaxInstructors, int? CustomMaxManagers = null, int? CustomMaxEmployees = null,
    bool? CustomCanExportData = null, bool? CustomHasAdvancedReports = null,
    bool? CustomHasAutomaticDuesReminders = null)
    : ICommand<Unit>;
