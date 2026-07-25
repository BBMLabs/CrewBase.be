using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.RegisterCompany;

public sealed record RegisterCompanyCommand(
    string CompanyName,
    string AdminEmail,
    string AdminPassword,
    string? Phone,
    string? ContactEmail,
    string? Address) : ICommand<RegisterCompanyResponse>;

public sealed record RegisterCompanyResponse(
    Guid CompanyId,
    Guid AdminUserId,
    string CompanyName,
    string AdminEmail,
    DateTimeOffset CreatedAtUtc);
