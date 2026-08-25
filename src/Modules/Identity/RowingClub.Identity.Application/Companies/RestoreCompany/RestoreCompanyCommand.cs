using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.RestoreCompany;

public sealed record RestoreCompanyCommand(Guid CompanyId) : ICommand<Unit>;
