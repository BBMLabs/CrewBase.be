using MediatR;

namespace RowingClub.Identity.Application.Companies.GetCompanySite;

/// <summary>Subdomain'den veya firma kimliğinden site/tenant bilgisini çözer (public site + panel için).</summary>
public sealed record GetCompanySiteQuery(string? Subdomain, Guid? CompanyId) : IRequest<CompanySiteDto?>;

public sealed record CompanySiteDto(
    Guid CompanyId,
    string Name,
    string Subdomain,
    string DatabaseName,
    string? Phone,
    string? ContactEmail,
    string? Address,
    string Status);
