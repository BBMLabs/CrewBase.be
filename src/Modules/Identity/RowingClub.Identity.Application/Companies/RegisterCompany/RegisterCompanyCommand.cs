using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.RegisterCompany;

public sealed record RegisterCompanyCommand(
    string CompanyName,
    string AdminEmail,
    string TaxNumber,
    string Phone,
    string ContactEmail,
    string Address,
    string? Subdomain = null) : ICommand<RegisterCompanyResponse>;

public sealed record RegisterCompanyResponse(
    Guid CompanyId,
    Guid AdminUserId,
    string CompanyName,
    string AdminEmail,
    string Subdomain,
    string SiteUrl,
    DateTimeOffset CreatedAtUtc);
