using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.ApproveCompany;

public sealed record ApproveCompanyCommand(Guid CompanyId, Guid ApprovedByUserId) : ICommand<Unit>;
