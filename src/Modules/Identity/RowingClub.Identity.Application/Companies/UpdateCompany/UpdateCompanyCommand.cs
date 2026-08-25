using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.UpdateCompany;

public sealed record UpdateCompanyCommand(
    Guid CompanyId, string Name, string? Phone, string? ContactEmail, string? Address, string? TaxNumber)
    : ICommand<Unit>;
