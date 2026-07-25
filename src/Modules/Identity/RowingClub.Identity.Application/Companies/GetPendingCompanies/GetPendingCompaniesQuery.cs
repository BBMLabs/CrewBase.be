using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.GetPendingCompanies;

public sealed record GetPendingCompaniesQuery : IQuery<List<PendingCompanyDto>>;

public sealed record PendingCompanyDto(
    Guid CompanyId,
    string Name,
    string? ContactEmail,
    string? Phone,
    string? Address,
    DateTimeOffset CreatedAtUtc);
