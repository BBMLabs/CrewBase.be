using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.SuspendCompany;

public sealed record SuspendCompanyCommand(Guid CompanyId) : ICommand<Unit>;
