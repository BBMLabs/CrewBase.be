using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.SoftDeleteCompany;

public sealed record SoftDeleteCompanyCommand(Guid CompanyId) : ICommand<Unit>;
